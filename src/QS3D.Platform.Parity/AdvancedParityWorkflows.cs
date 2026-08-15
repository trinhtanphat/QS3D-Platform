using System.Collections.ObjectModel;

namespace QS3D.Platform.Parity;

public sealed class NamedMepRecognitionProfile
{
    public NamedMepRecognitionProfile(string profileId, string name, MepRecognitionProfile profile, bool isDefault = false)
    {
        ProfileId = Text.Require(profileId, nameof(profileId));
        Name = Text.Require(name, nameof(name));
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        IsDefault = isDefault;
    }

    public string ProfileId { get; }
    public string Name { get; }
    public MepRecognitionProfile Profile { get; }
    public bool IsDefault { get; }
}

public sealed class MepRecognitionProfileCatalog
{
    private readonly Dictionary<string, NamedMepRecognitionProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<NamedMepRecognitionProfile> Profiles =>
        new ReadOnlyCollection<NamedMepRecognitionProfile>(_profiles.Values.OrderBy(static x => x.ProfileId, StringComparer.OrdinalIgnoreCase).ToList());

    public void Add(NamedMepRecognitionProfile profile)
    {
        if (profile is null) throw new ArgumentNullException(nameof(profile));
        if (_profiles.ContainsKey(profile.ProfileId)) throw new InvalidOperationException("Duplicate recognition profile id: " + profile.ProfileId + ".");
        if (profile.IsDefault && _profiles.Values.Any(static x => x.IsDefault)) throw new InvalidOperationException("Only one recognition profile may be default.");
        _profiles.Add(profile.ProfileId, profile);
    }

    public NamedMepRecognitionProfile? Find(string profileId)
    {
        profileId = Text.Require(profileId, nameof(profileId));
        return _profiles.TryGetValue(profileId, out var profile) ? profile : null;
    }

    public NamedMepRecognitionProfile? Default => _profiles.Values.SingleOrDefault(static x => x.IsDefault);
}

public sealed class RateApplicationCandidate
{
    public RateApplicationCandidate(string itemCode, string unit, string dimensionKey, decimal unitRate, string sourceId, int priority = 0)
    {
        ItemCode = Text.Require(itemCode, nameof(itemCode));
        Unit = Text.Require(unit, nameof(unit));
        DimensionKey = Text.Require(dimensionKey, nameof(dimensionKey));
        if (unitRate < 0m) throw new ArgumentOutOfRangeException(nameof(unitRate));
        UnitRate = unitRate;
        SourceId = Text.Require(sourceId, nameof(sourceId));
        Priority = priority;
    }

    public string ItemCode { get; }
    public string Unit { get; }
    public string DimensionKey { get; }
    public decimal UnitRate { get; }
    public string SourceId { get; }
    public int Priority { get; }
}

public sealed class RateApplicationRequest
{
    public RateApplicationRequest(string itemCode, string unit, string dimensionKey)
    {
        ItemCode = Text.Require(itemCode, nameof(itemCode));
        Unit = Text.Require(unit, nameof(unit));
        DimensionKey = Text.Require(dimensionKey, nameof(dimensionKey));
    }

    public string ItemCode { get; }
    public string Unit { get; }
    public string DimensionKey { get; }
}

public enum RateApplicationStatus
{
    Unmatched = 0,
    Matched = 1,
    Ambiguous = 2
}

public sealed class RateApplicationResult
{
    internal RateApplicationResult(RateApplicationStatus status, decimal? unitRate, string? sourceId, IReadOnlyList<string> candidateSourceIds)
    {
        Status = status;
        UnitRate = unitRate;
        SourceId = sourceId;
        CandidateSourceIds = candidateSourceIds;
    }

    public RateApplicationStatus Status { get; }
    public decimal? UnitRate { get; }
    public string? SourceId { get; }
    public IReadOnlyList<string> CandidateSourceIds { get; }
}

public sealed class SmartRateApplicationService
{
    public RateApplicationResult Match(RateApplicationRequest request, IEnumerable<RateApplicationCandidate> candidates)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (candidates is null) throw new ArgumentNullException(nameof(candidates));
        var matches = new List<RateApplicationCandidate>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (candidate is null) throw new ArgumentException("Rate candidates contain null.", nameof(candidates));
            if (!ids.Add(candidate.SourceId)) throw new ArgumentException("Duplicate rate source id: " + candidate.SourceId + ".", nameof(candidates));
            if (!StringComparer.OrdinalIgnoreCase.Equals(candidate.ItemCode, request.ItemCode) ||
                !StringComparer.OrdinalIgnoreCase.Equals(candidate.Unit, request.Unit) ||
                !StringComparer.OrdinalIgnoreCase.Equals(candidate.DimensionKey, request.DimensionKey)) continue;
            matches.Add(candidate);
        }
        if (matches.Count == 0) return new RateApplicationResult(RateApplicationStatus.Unmatched, null, null, Array.Empty<string>());
        var highest = matches.Max(static x => x.Priority);
        var top = matches.Where(x => x.Priority == highest).OrderBy(static x => x.SourceId, StringComparer.OrdinalIgnoreCase).ToList();
        var sourceIds = new ReadOnlyCollection<string>(top.Select(static x => x.SourceId).ToList());
        var firstRate = top[0].UnitRate;
        if (top.Any(x => x.UnitRate != firstRate)) return new RateApplicationResult(RateApplicationStatus.Ambiguous, null, null, sourceIds);
        return new RateApplicationResult(RateApplicationStatus.Matched, firstRate, top[0].SourceId, sourceIds);
    }
}

public sealed class TenderRevisionLine
{
    public TenderRevisionLine(string itemCode, string description, string unit, decimal quantity)
    {
        ItemCode = Text.Require(itemCode, nameof(itemCode));
        Description = Text.Require(description, nameof(description));
        Unit = Text.Require(unit, nameof(unit));
        if (quantity < 0m) throw new ArgumentOutOfRangeException(nameof(quantity));
        Quantity = quantity;
    }

    public string ItemCode { get; }
    public string Description { get; }
    public string Unit { get; }
    public decimal Quantity { get; }
}

public enum TenderRevisionChangeKind
{
    Added = 0,
    Removed = 1,
    Changed = 2
}

public sealed class TenderRevisionChange
{
    internal TenderRevisionChange(string itemCode, TenderRevisionChangeKind kind, TenderRevisionLine? before, TenderRevisionLine? after)
    {
        ItemCode = itemCode;
        Kind = kind;
        Before = before;
        After = after;
    }

    public string ItemCode { get; }
    public TenderRevisionChangeKind Kind { get; }
    public TenderRevisionLine? Before { get; }
    public TenderRevisionLine? After { get; }
}

public static class TenderRevisionService
{
    public static IReadOnlyList<TenderRevisionChange> Compare(IEnumerable<TenderRevisionLine> before, IEnumerable<TenderRevisionLine> after)
    {
        var left = Index(before, nameof(before));
        var right = Index(after, nameof(after));
        var keys = new SortedSet<string>(left.Keys, StringComparer.OrdinalIgnoreCase);
        keys.UnionWith(right.Keys);
        var changes = new List<TenderRevisionChange>();
        foreach (var key in keys)
        {
            left.TryGetValue(key, out var oldLine);
            right.TryGetValue(key, out var newLine);
            if (oldLine is null)
            {
                changes.Add(new TenderRevisionChange(key, TenderRevisionChangeKind.Added, null, newLine));
                continue;
            }
            if (newLine is null)
            {
                changes.Add(new TenderRevisionChange(key, TenderRevisionChangeKind.Removed, oldLine, null));
                continue;
            }
            if (!Equivalent(oldLine, newLine))
                changes.Add(new TenderRevisionChange(key, TenderRevisionChangeKind.Changed, oldLine, newLine));
        }
        return new ReadOnlyCollection<TenderRevisionChange>(changes);
    }

    private static Dictionary<string, TenderRevisionLine> Index(IEnumerable<TenderRevisionLine> source, string parameterName)
    {
        if (source is null) throw new ArgumentNullException(parameterName);
        var result = new Dictionary<string, TenderRevisionLine>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in source)
        {
            if (line is null) throw new ArgumentException("Tender revision contains null.", parameterName);
            if (result.ContainsKey(line.ItemCode)) throw new ArgumentException("Duplicate tender revision item: " + line.ItemCode + ".", parameterName);
            result.Add(line.ItemCode, line);
        }
        return result;
    }

    private static bool Equivalent(TenderRevisionLine left, TenderRevisionLine right) =>
        StringComparer.Ordinal.Equals(left.Description, right.Description) &&
        StringComparer.OrdinalIgnoreCase.Equals(left.Unit, right.Unit) &&
        left.Quantity == right.Quantity;
}

public sealed class TenderRound
{
    public TenderRound(string roundId, DateTime openedAtUtc, IEnumerable<TenderBid> bids)
    {
        RoundId = Text.Require(roundId, nameof(roundId));
        if (openedAtUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Tender round timestamp must be UTC.", nameof(openedAtUtc));
        OpenedAtUtc = openedAtUtc;
        if (bids is null) throw new ArgumentNullException(nameof(bids));
        var list = new List<TenderBid>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var bid in bids)
        {
            if (bid is null) throw new ArgumentException("Tender round contains null bid.", nameof(bids));
            if (!ids.Add(bid.BidId)) throw new ArgumentException("Duplicate bid id in tender round: " + bid.BidId + ".", nameof(bids));
            list.Add(bid);
        }
        Bids = new ReadOnlyCollection<TenderBid>(list);
    }

    public string RoundId { get; }
    public DateTime OpenedAtUtc { get; }
    public IReadOnlyList<TenderBid> Bids { get; }
}

public sealed class TenderRoundEvaluation
{
    internal TenderRoundEvaluation(string roundId, DateTime openedAtUtc, IReadOnlyList<TenderEvaluationResult> results)
    {
        RoundId = roundId;
        OpenedAtUtc = openedAtUtc;
        Results = results;
    }

    public string RoundId { get; }
    public DateTime OpenedAtUtc { get; }
    public IReadOnlyList<TenderEvaluationResult> Results { get; }
}

public sealed class MultiRoundTenderEvaluationService
{
    public IReadOnlyList<TenderRoundEvaluation> Evaluate(IEnumerable<TenderRequirement> requirements, IEnumerable<TenderRound> rounds)
    {
        if (requirements is null) throw new ArgumentNullException(nameof(requirements));
        if (rounds is null) throw new ArgumentNullException(nameof(rounds));
        var requirementSnapshot = requirements.ToArray();
        var list = new List<TenderRound>();
        var roundIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var round in rounds)
        {
            if (round is null) throw new ArgumentException("Tender rounds contain null.", nameof(rounds));
            if (!roundIds.Add(round.RoundId)) throw new ArgumentException("Duplicate tender round id: " + round.RoundId + ".", nameof(rounds));
            list.Add(round);
        }
        list.Sort(static (left, right) =>
        {
            var time = left.OpenedAtUtc.CompareTo(right.OpenedAtUtc);
            return time != 0 ? time : StringComparer.OrdinalIgnoreCase.Compare(left.RoundId, right.RoundId);
        });
        var evaluator = new TenderEvaluationService();
        var result = new List<TenderRoundEvaluation>(list.Count);
        for (var i = 0; i < list.Count; i++)
            result.Add(new TenderRoundEvaluation(list[i].RoundId, list[i].OpenedAtUtc, evaluator.Evaluate(requirementSnapshot, list[i].Bids)));
        return new ReadOnlyCollection<TenderRoundEvaluation>(result);
    }
}

using System.Collections.ObjectModel;

namespace QS3D.Platform.Parity;

public sealed class BuildUpAnalysisChange
{
    internal BuildUpAnalysisChange(
        CostRateBuildUp previous,
        CostRateBuildUp current,
        IReadOnlyList<string> affectedBqItemCodes,
        BuildUpAnalysisWorkspace workspace)
    {
        Previous = previous;
        Current = current;
        AffectedBqItemCodes = affectedBqItemCodes;
        Workspace = workspace;
    }

    public CostRateBuildUp Previous { get; }
    public CostRateBuildUp Current { get; }
    public IReadOnlyList<string> AffectedBqItemCodes { get; }
    public BuildUpAnalysisWorkspace Workspace { get; }
}

public sealed class BuildUpAnalysisWorkspace
{
    private readonly IReadOnlyList<CostRateBuildUp> _rates;
    private readonly IReadOnlyList<BqRateAdoption> _adoptions;
    private readonly Dictionary<string, CostRateBuildUp> _rateById;
    private readonly Dictionary<string, IReadOnlyList<string>> _bqItemsByRate;

    public BuildUpAnalysisWorkspace(IEnumerable<CostRateBuildUp> rates, IEnumerable<BqRateAdoption> bqAdoptions)
    {
        if (rates is null) throw new ArgumentNullException(nameof(rates));
        if (bqAdoptions is null) throw new ArgumentNullException(nameof(bqAdoptions));

        var allRates = new Dictionary<string, CostRateBuildUp>(StringComparer.OrdinalIgnoreCase);
        foreach (var rate in rates)
        {
            if (rate is null) throw new ArgumentException("Build-up analysis contains a null rate.", nameof(rates));
            if (allRates.ContainsKey(rate.Id))
                throw new ArgumentException("Duplicate build-up rate id: " + rate.Id + ".", nameof(rates));
            allRates.Add(rate.Id, rate);
        }

        var adoptionSnapshot = new List<BqRateAdoption>();
        var adoptionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var adoptedRateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var adoption in bqAdoptions)
        {
            if (adoption is null) throw new ArgumentException("Build-up analysis contains a null BQ adoption.", nameof(bqAdoptions));
            if (!allRates.ContainsKey(adoption.RateId))
                throw new ArgumentException("BQ adoption references an unknown build-up rate: " + adoption.RateId + ".", nameof(bqAdoptions));
            var key = adoption.BqItemCode + "\u001F" + adoption.RateId;
            if (!adoptionKeys.Add(key))
                throw new ArgumentException("Duplicate BQ/rate adoption: " + adoption.BqItemCode + " -> " + adoption.RateId + ".", nameof(bqAdoptions));
            adoptionSnapshot.Add(adoption);
            adoptedRateIds.Add(adoption.RateId);
        }
        adoptionSnapshot.Sort(CompareAdoptions);
        _adoptions = new ReadOnlyCollection<BqRateAdoption>(adoptionSnapshot);

        var adoptedRates = allRates.Values.Where(rate => adoptedRateIds.Contains(rate.Id)).ToList();
        adoptedRates.Sort(static (left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Id, right.Id));
        _rates = new ReadOnlyCollection<CostRateBuildUp>(adoptedRates);
        _rateById = adoptedRates.ToDictionary(static rate => rate.Id, StringComparer.OrdinalIgnoreCase);
        _bqItemsByRate = BuildReverseIndex(adoptionSnapshot);
    }

    public IReadOnlyList<CostRateBuildUp> Rates => _rates;
    public IReadOnlyList<BqRateAdoption> BqAdoptions => _adoptions;

    public IReadOnlyList<string> CheckBqReversely(string rateId)
    {
        rateId = Text.Require(rateId, nameof(rateId));
        if (!_rateById.ContainsKey(rateId))
            throw new InvalidOperationException("Rate is not available in Build-up Analysis because it is not adopted in BQ: " + rateId + ".");
        return _bqItemsByRate[rateId];
    }

    public BuildUpAnalysisChange UpdateExisting(CostRateBuildUp replacement)
    {
        if (replacement is null) throw new ArgumentNullException(nameof(replacement));
        if (!_rateById.TryGetValue(replacement.Id, out var previous))
            throw new InvalidOperationException("Build-up Analysis cannot add or update a rate that is not already adopted in BQ: " + replacement.Id + ".");

        var nextRates = new List<CostRateBuildUp>(_rates.Count);
        for (var i = 0; i < _rates.Count; i++)
        {
            var rate = _rates[i];
            nextRates.Add(StringComparer.OrdinalIgnoreCase.Equals(rate.Id, replacement.Id) ? replacement : rate);
        }

        var next = new BuildUpAnalysisWorkspace(nextRates, _adoptions);
        return new BuildUpAnalysisChange(previous, replacement, next.CheckBqReversely(replacement.Id), next);
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildReverseIndex(IEnumerable<BqRateAdoption> adoptions)
    {
        var mutable = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var adoption in adoptions)
        {
            if (!mutable.TryGetValue(adoption.RateId, out var items))
            {
                items = new List<string>();
                mutable.Add(adoption.RateId, items);
            }
            items.Add(adoption.BqItemCode);
        }

        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in mutable)
        {
            item.Value.Sort(StringComparer.OrdinalIgnoreCase);
            result.Add(item.Key, new ReadOnlyCollection<string>(item.Value));
        }
        return result;
    }

    private static int CompareAdoptions(BqRateAdoption left, BqRateAdoption right)
    {
        var rate = StringComparer.OrdinalIgnoreCase.Compare(left.RateId, right.RateId);
        return rate != 0 ? rate : StringComparer.OrdinalIgnoreCase.Compare(left.BqItemCode, right.BqItemCode);
    }
}

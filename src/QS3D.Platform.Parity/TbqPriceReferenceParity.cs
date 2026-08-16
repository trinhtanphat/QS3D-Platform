using System.Collections.ObjectModel;

namespace QS3D.Platform.Parity;

public enum CostRateKind
{
    UnitRate = 0,
    CompositeMaterialLabor = 1,
    Material = 2,
    Labor = 3,
    Equipment = 4,
    Other = 5
}

[Flags]
public enum CostReferenceUsage
{
    None = 0,
    Bq = 1,
    UnitRate = 2
}

public sealed class CostRateNode
{
    public CostRateNode(string rateId, string description, CostRateKind kind)
    {
        RateId = Text.Require(rateId, nameof(rateId));
        Description = Text.Require(description, nameof(description));
        if (!Enum.IsDefined(typeof(CostRateKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind;
    }

    public string RateId { get; }
    public string Description { get; }
    public CostRateKind Kind { get; }
}

public sealed class CostRateCompositionLink
{
    public CostRateCompositionLink(string unitRateId, string componentRateId)
    {
        UnitRateId = Text.Require(unitRateId, nameof(unitRateId));
        ComponentRateId = Text.Require(componentRateId, nameof(componentRateId));
        if (StringComparer.OrdinalIgnoreCase.Equals(UnitRateId, ComponentRateId))
            throw new ArgumentException("A unit rate cannot reference itself as a basic rate.", nameof(componentRateId));
    }

    public string UnitRateId { get; }
    public string ComponentRateId { get; }
}

public sealed class BqRateAdoption
{
    public BqRateAdoption(string bqItemCode, string rateId)
    {
        BqItemCode = Text.Require(bqItemCode, nameof(bqItemCode));
        RateId = Text.Require(rateId, nameof(rateId));
    }

    public string BqItemCode { get; }
    public string RateId { get; }
}

public sealed class CostRateReferenceState
{
    internal CostRateReferenceState(string rateId, CostReferenceUsage usage)
    {
        RateId = rateId;
        Usage = usage;
    }

    public string RateId { get; }
    public CostReferenceUsage Usage { get; }
    public bool IsAdoptedInBq => (Usage & CostReferenceUsage.Bq) != 0;
    public bool IsAdoptedInUnitRate => (Usage & CostReferenceUsage.UnitRate) != 0;
    public string ReferenceMark => Usage switch
    {
        CostReferenceUsage.Bq => "BQ",
        CostReferenceUsage.UnitRate => "UR",
        CostReferenceUsage.Bq | CostReferenceUsage.UnitRate => "BQ+UR",
        _ => string.Empty
    };
}

public sealed class CostRateReferenceGraph
{
    private readonly IReadOnlyList<CostRateNode> _rates;
    private readonly IReadOnlyList<CostRateCompositionLink> _compositionLinks;
    private readonly IReadOnlyList<BqRateAdoption> _bqAdoptions;
    private readonly Dictionary<string, CostRateNode> _rateById;
    private readonly Dictionary<string, IReadOnlyList<string>> _unitRatesByComponent;
    private readonly Dictionary<string, IReadOnlyList<string>> _bqItemsByRate;

    public CostRateReferenceGraph(
        IEnumerable<CostRateNode> rates,
        IEnumerable<CostRateCompositionLink>? compositionLinks = null,
        IEnumerable<BqRateAdoption>? bqAdoptions = null)
    {
        if (rates is null) throw new ArgumentNullException(nameof(rates));

        _rateById = new Dictionary<string, CostRateNode>(StringComparer.OrdinalIgnoreCase);
        var rateSnapshot = new List<CostRateNode>();
        foreach (var rate in rates)
        {
            if (rate is null) throw new ArgumentException("Rate graph contains a null rate.", nameof(rates));
            if (_rateById.ContainsKey(rate.RateId))
                throw new ArgumentException("Duplicate rate id: " + rate.RateId + ".", nameof(rates));
            _rateById.Add(rate.RateId, rate);
            rateSnapshot.Add(rate);
        }
        rateSnapshot.Sort(static (left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.RateId, right.RateId));
        _rates = new ReadOnlyCollection<CostRateNode>(rateSnapshot);

        var compositionSnapshot = new List<CostRateCompositionLink>();
        var compositionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var link in compositionLinks ?? Array.Empty<CostRateCompositionLink>())
        {
            if (link is null) throw new ArgumentException("Rate composition contains a null link.", nameof(compositionLinks));
            var unitRate = RequireRate(link.UnitRateId, nameof(compositionLinks));
            var component = RequireRate(link.ComponentRateId, nameof(compositionLinks));
            if (unitRate.Kind != CostRateKind.UnitRate)
                throw new ArgumentException("Composition parent must be a unit rate: " + link.UnitRateId + ".", nameof(compositionLinks));
            if (component.Kind == CostRateKind.UnitRate)
                throw new ArgumentException("Composition component must be a basic rate: " + link.ComponentRateId + ".", nameof(compositionLinks));
            var key = link.UnitRateId + "\u001F" + link.ComponentRateId;
            if (!compositionKeys.Add(key))
                throw new ArgumentException("Duplicate rate composition link: " + link.UnitRateId + " -> " + link.ComponentRateId + ".", nameof(compositionLinks));
            compositionSnapshot.Add(link);
        }
        compositionSnapshot.Sort(CompareCompositionLinks);
        _compositionLinks = new ReadOnlyCollection<CostRateCompositionLink>(compositionSnapshot);

        var bqSnapshot = new List<BqRateAdoption>();
        var bqKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var adoption in bqAdoptions ?? Array.Empty<BqRateAdoption>())
        {
            if (adoption is null) throw new ArgumentException("BQ adoption contains a null link.", nameof(bqAdoptions));
            _ = RequireRate(adoption.RateId, nameof(bqAdoptions));
            var key = adoption.BqItemCode + "\u001F" + adoption.RateId;
            if (!bqKeys.Add(key))
                throw new ArgumentException("Duplicate BQ/rate adoption: " + adoption.BqItemCode + " -> " + adoption.RateId + ".", nameof(bqAdoptions));
            bqSnapshot.Add(adoption);
        }
        bqSnapshot.Sort(CompareBqAdoptions);
        _bqAdoptions = new ReadOnlyCollection<BqRateAdoption>(bqSnapshot);

        _unitRatesByComponent = BuildUnitRateReverseIndex(compositionSnapshot);
        _bqItemsByRate = BuildBqReverseIndex(bqSnapshot);
    }

    public IReadOnlyList<CostRateNode> Rates => _rates;
    public IReadOnlyList<CostRateCompositionLink> CompositionLinks => _compositionLinks;
    public IReadOnlyList<BqRateAdoption> BqAdoptions => _bqAdoptions;

    public CostRateReferenceState GetReferenceState(string rateId)
    {
        rateId = Text.Require(rateId, nameof(rateId));
        var rate = RequireRate(rateId, nameof(rateId));
        var usage = CostReferenceUsage.None;
        if (_bqItemsByRate.ContainsKey(rateId)) usage |= CostReferenceUsage.Bq;
        if (_unitRatesByComponent.ContainsKey(rateId)) usage |= CostReferenceUsage.UnitRate;
        return new CostRateReferenceState(rate.RateId, usage);
    }

    public IReadOnlyList<string> CheckLinkingRates(string basicRateId)
    {
        basicRateId = Text.Require(basicRateId, nameof(basicRateId));
        var rate = RequireRate(basicRateId, nameof(basicRateId));
        if (rate.Kind == CostRateKind.UnitRate)
            throw new ArgumentException("Check Linking Rate expects a basic rate, not a unit rate.", nameof(basicRateId));
        return _unitRatesByComponent.TryGetValue(basicRateId, out var unitRateIds)
            ? unitRateIds
            : Array.Empty<string>();
    }

    public IReadOnlyList<string> CheckBqReversely(string rateId)
    {
        rateId = Text.Require(rateId, nameof(rateId));
        _ = RequireRate(rateId, nameof(rateId));
        return _bqItemsByRate.TryGetValue(rateId, out var bqItemCodes)
            ? bqItemCodes
            : Array.Empty<string>();
    }

    public IReadOnlyList<CostRateNode> FindRatesNotAdoptedInBq()
    {
        var result = _rates.Where(rate => !_bqItemsByRate.ContainsKey(rate.RateId)).ToList();
        return new ReadOnlyCollection<CostRateNode>(result);
    }

    private CostRateNode RequireRate(string rateId, string parameterName)
    {
        if (!_rateById.TryGetValue(rateId, out var rate))
            throw new ArgumentException("Unknown rate id: " + rateId + ".", parameterName);
        return rate;
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildUnitRateReverseIndex(IEnumerable<CostRateCompositionLink> links)
    {
        var mutable = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var link in links)
        {
            if (!mutable.TryGetValue(link.ComponentRateId, out var unitRateIds))
            {
                unitRateIds = new List<string>();
                mutable.Add(link.ComponentRateId, unitRateIds);
            }
            unitRateIds.Add(link.UnitRateId);
        }
        return Freeze(mutable);
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildBqReverseIndex(IEnumerable<BqRateAdoption> adoptions)
    {
        var mutable = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var adoption in adoptions)
        {
            if (!mutable.TryGetValue(adoption.RateId, out var bqItemCodes))
            {
                bqItemCodes = new List<string>();
                mutable.Add(adoption.RateId, bqItemCodes);
            }
            bqItemCodes.Add(adoption.BqItemCode);
        }
        return Freeze(mutable);
    }

    private static Dictionary<string, IReadOnlyList<string>> Freeze(Dictionary<string, List<string>> source)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in source)
        {
            item.Value.Sort(StringComparer.OrdinalIgnoreCase);
            result.Add(item.Key, new ReadOnlyCollection<string>(item.Value));
        }
        return result;
    }

    private static int CompareCompositionLinks(CostRateCompositionLink left, CostRateCompositionLink right)
    {
        var parent = StringComparer.OrdinalIgnoreCase.Compare(left.UnitRateId, right.UnitRateId);
        return parent != 0 ? parent : StringComparer.OrdinalIgnoreCase.Compare(left.ComponentRateId, right.ComponentRateId);
    }

    private static int CompareBqAdoptions(BqRateAdoption left, BqRateAdoption right)
    {
        var item = StringComparer.OrdinalIgnoreCase.Compare(left.BqItemCode, right.BqItemCode);
        return item != 0 ? item : StringComparer.OrdinalIgnoreCase.Compare(left.RateId, right.RateId);
    }
}

using System.Collections.ObjectModel;

namespace QS3D.Platform.Parity;

public enum MepBqMeasurementBasis
{
    Count = 0,
    Length = 1,
    Area = 2,
    Volume = 3
}

public enum MepBqMappingStatus
{
    Unmatched = 0,
    Matched = 1,
    Ambiguous = 2
}

public sealed class MepBqMappingRule
{
    public MepBqMappingRule(
        string id,
        int priority,
        string itemCode,
        MepBqMeasurementBasis measurementBasis,
        MepElementKind? kind = null,
        string? system = null,
        string? specification = null,
        string? region = null)
    {
        Id = Text.Require(id, nameof(id));
        Priority = priority;
        ItemCode = Text.Require(itemCode, nameof(itemCode));
        if (!Enum.IsDefined(typeof(MepBqMeasurementBasis), measurementBasis)) throw new ArgumentOutOfRangeException(nameof(measurementBasis));
        MeasurementBasis = measurementBasis;
        if (kind.HasValue && !Enum.IsDefined(typeof(MepElementKind), kind.Value)) throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind;
        System = Optional(system, nameof(system));
        Specification = Optional(specification, nameof(specification));
        Region = Optional(region, nameof(region));
    }

    public string Id { get; }
    public int Priority { get; }
    public string ItemCode { get; }
    public MepBqMeasurementBasis MeasurementBasis { get; }
    public MepElementKind? Kind { get; }
    public string? System { get; }
    public string? Specification { get; }
    public string? Region { get; }

    internal bool Matches(MepQuantityGroup group)
    {
        if (Kind.HasValue && Kind.Value != group.Kind) return false;
        if (System is not null && !StringComparer.OrdinalIgnoreCase.Equals(System, group.System)) return false;
        if (Specification is not null && !StringComparer.OrdinalIgnoreCase.Equals(Specification, group.Specification)) return false;
        if (Region is not null && !StringComparer.OrdinalIgnoreCase.Equals(Region, group.Region)) return false;
        return true;
    }

    private static string? Optional(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Text.Require(value, parameterName);
    }
}

public sealed class MepBqMappingMatch
{
    internal MepBqMappingMatch(
        MepBqMappingStatus status,
        string? itemCode,
        MepBqMeasurementBasis? measurementBasis,
        IReadOnlyList<string> matchedRuleIds)
    {
        Status = status;
        ItemCode = itemCode;
        MeasurementBasis = measurementBasis;
        MatchedRuleIds = matchedRuleIds;
    }

    public MepBqMappingStatus Status { get; }
    public string? ItemCode { get; }
    public MepBqMeasurementBasis? MeasurementBasis { get; }
    public IReadOnlyList<string> MatchedRuleIds { get; }
}

public sealed class MepBqMappingProfile
{
    private readonly IReadOnlyList<MepBqMappingRule> _rules;

    public MepBqMappingProfile(IEnumerable<MepBqMappingRule> rules)
    {
        if (rules is null) throw new ArgumentNullException(nameof(rules));
        var snapshot = new List<MepBqMappingRule>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules)
        {
            if (rule is null) throw new ArgumentException("MEP-to-BQ mapping profile contains null.", nameof(rules));
            if (!ids.Add(rule.Id)) throw new ArgumentException("Duplicate MEP-to-BQ rule id: " + rule.Id + ".", nameof(rules));
            snapshot.Add(rule);
        }
        if (snapshot.Count == 0) throw new ArgumentException("MEP-to-BQ mapping profile must contain at least one rule.", nameof(rules));
        snapshot.Sort(static (left, right) =>
        {
            var priority = right.Priority.CompareTo(left.Priority);
            return priority != 0 ? priority : StringComparer.OrdinalIgnoreCase.Compare(left.Id, right.Id);
        });
        _rules = new ReadOnlyCollection<MepBqMappingRule>(snapshot);
    }

    public IReadOnlyList<MepBqMappingRule> Rules => _rules;

    public MepBqMappingMatch Match(MepQuantityGroup group)
    {
        if (group is null) throw new ArgumentNullException(nameof(group));
        var top = new List<MepBqMappingRule>();
        var priority = int.MinValue;
        for (var i = 0; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            if (!rule.Matches(group)) continue;
            if (rule.Priority < priority) break;
            if (rule.Priority > priority)
            {
                priority = rule.Priority;
                top.Clear();
            }
            top.Add(rule);
        }

        if (top.Count == 0)
            return new MepBqMappingMatch(MepBqMappingStatus.Unmatched, null, null, Array.Empty<string>());

        var first = top[0];
        var ambiguous = false;
        for (var i = 1; i < top.Count; i++)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(first.ItemCode, top[i].ItemCode) ||
                first.MeasurementBasis != top[i].MeasurementBasis)
            {
                ambiguous = true;
                break;
            }
        }
        var ids = new string[top.Count];
        for (var i = 0; i < top.Count; i++) ids[i] = top[i].Id;
        return ambiguous
            ? new MepBqMappingMatch(MepBqMappingStatus.Ambiguous, null, null, ids)
            : new MepBqMappingMatch(MepBqMappingStatus.Matched, first.ItemCode, first.MeasurementBasis, ids);
    }
}

public sealed class MepBqSourceGroup
{
    internal MepBqSourceGroup(MepQuantityGroup group, decimal contributedQuantity)
    {
        Region = group.Region;
        System = group.System;
        Specification = group.Specification;
        Kind = group.Kind;
        ElementCount = group.ElementCount;
        QuantityCount = group.QuantityCount;
        ContributedQuantity = contributedQuantity;
    }

    public string Region { get; }
    public string System { get; }
    public string Specification { get; }
    public MepElementKind Kind { get; }
    public int ElementCount { get; }
    public int QuantityCount { get; }
    public decimal ContributedQuantity { get; }
}

public sealed class MepBqProjectionLine
{
    internal MepBqProjectionLine(
        string itemCode,
        string description,
        string unit,
        string categoryPath,
        MepBqMeasurementBasis measurementBasis,
        decimal quantity,
        IReadOnlyList<MepBqSourceGroup> sources)
    {
        ItemCode = itemCode;
        Description = description;
        Unit = unit;
        CategoryPath = categoryPath;
        MeasurementBasis = measurementBasis;
        Quantity = quantity;
        Sources = sources;
    }

    public string ItemCode { get; }
    public string Description { get; }
    public string Unit { get; }
    public string CategoryPath { get; }
    public MepBqMeasurementBasis MeasurementBasis { get; }
    public decimal Quantity { get; }
    public IReadOnlyList<MepBqSourceGroup> Sources { get; }
}

public sealed class MepBqProjectionService
{
    public IReadOnlyList<MepBqProjectionLine> Project(
        IEnumerable<MepQuantityGroup> groups,
        MepBqMappingProfile profile,
        BqLibraryCatalog library,
        bool requireAllMapped = true)
    {
        if (groups is null) throw new ArgumentNullException(nameof(groups));
        if (profile is null) throw new ArgumentNullException(nameof(profile));
        if (library is null) throw new ArgumentNullException(nameof(library));

        var sourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var accumulators = new Dictionary<string, MutableLine>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            if (group is null) throw new ArgumentException("MEP quantity projection contains null group.", nameof(groups));
            var sourceKey = SourceKey(group);
            if (!sourceKeys.Add(sourceKey)) throw new ArgumentException("Duplicate MEP quantity source group: " + sourceKey + ".", nameof(groups));

            var match = profile.Match(group);
            if (match.Status == MepBqMappingStatus.Unmatched)
            {
                if (requireAllMapped) throw new InvalidOperationException("No MEP-to-BQ mapping rule matched source group " + sourceKey + ".");
                continue;
            }
            if (match.Status == MepBqMappingStatus.Ambiguous || match.ItemCode is null || !match.MeasurementBasis.HasValue)
                throw new InvalidOperationException("Ambiguous MEP-to-BQ mapping for source group " + sourceKey + ": " + string.Join(", ", match.MatchedRuleIds) + ".");

            var item = library.Find(match.ItemCode);
            if (item is null) throw new InvalidOperationException("Mapped BQ item does not exist in the library: " + match.ItemCode + ".");
            var expectedUnit = UnitFor(match.MeasurementBasis.Value);
            if (!StringComparer.OrdinalIgnoreCase.Equals(item.Unit, expectedUnit))
                throw new InvalidOperationException("BQ item " + item.ItemCode + " unit must be exactly " + expectedUnit + " for " + match.MeasurementBasis.Value + " measurement.");

            var quantity = QuantityFor(group, match.MeasurementBasis.Value);
            if (!accumulators.TryGetValue(item.ItemCode, out var line))
            {
                line = new MutableLine(item, match.MeasurementBasis.Value);
                accumulators.Add(item.ItemCode, line);
            }
            else if (line.MeasurementBasis != match.MeasurementBasis.Value)
            {
                throw new InvalidOperationException("BQ item " + item.ItemCode + " is mapped with conflicting measurement bases across MEP groups.");
            }
            line.Add(group, quantity);
        }

        var result = accumulators.Values.Select(static line => line.ToImmutable()).ToList();
        result.Sort(static (left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.ItemCode, right.ItemCode));
        return new ReadOnlyCollection<MepBqProjectionLine>(result);
    }

    public static string UnitFor(MepBqMeasurementBasis basis) => basis switch
    {
        MepBqMeasurementBasis.Count => "ea",
        MepBqMeasurementBasis.Length => "m",
        MepBqMeasurementBasis.Area => "m2",
        MepBqMeasurementBasis.Volume => "m3",
        _ => throw new ArgumentOutOfRangeException(nameof(basis))
    };

    private static decimal QuantityFor(MepQuantityGroup group, MepBqMeasurementBasis basis) => basis switch
    {
        MepBqMeasurementBasis.Count => group.QuantityCount,
        MepBqMeasurementBasis.Length => ToDecimal(group.LengthM, "MEP length"),
        MepBqMeasurementBasis.Area => ToDecimal(group.AreaM2, "MEP area"),
        MepBqMeasurementBasis.Volume => ToDecimal(group.VolumeM3, "MEP volume"),
        _ => throw new ArgumentOutOfRangeException(nameof(basis))
    };

    private static decimal ToDecimal(double value, string label)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new InvalidOperationException(label + " must be finite and non-negative.");
        try { return checked((decimal)value); }
        catch (OverflowException) { throw new OverflowException(label + " exceeds decimal projection range."); }
    }

    private static string SourceKey(MepQuantityGroup group) =>
        group.Region + "|" + group.System + "|" + group.Specification + "|" + group.Kind;

    private sealed class MutableLine
    {
        private readonly BqLibraryItem _item;
        private readonly List<MepBqSourceGroup> _sources = new();
        private decimal _quantity;

        internal MutableLine(BqLibraryItem item, MepBqMeasurementBasis measurementBasis)
        {
            _item = item;
            MeasurementBasis = measurementBasis;
        }

        internal MepBqMeasurementBasis MeasurementBasis { get; }

        internal void Add(MepQuantityGroup group, decimal quantity)
        {
            _quantity = checked(_quantity + quantity);
            _sources.Add(new MepBqSourceGroup(group, quantity));
        }

        internal MepBqProjectionLine ToImmutable()
        {
            _sources.Sort(static (left, right) =>
            {
                var compare = StringComparer.OrdinalIgnoreCase.Compare(left.Region, right.Region);
                if (compare != 0) return compare;
                compare = StringComparer.OrdinalIgnoreCase.Compare(left.System, right.System);
                if (compare != 0) return compare;
                compare = StringComparer.OrdinalIgnoreCase.Compare(left.Specification, right.Specification);
                return compare != 0 ? compare : left.Kind.CompareTo(right.Kind);
            });
            return new MepBqProjectionLine(
                _item.ItemCode,
                _item.Description,
                _item.Unit,
                _item.CategoryPath,
                MeasurementBasis,
                _quantity,
                new ReadOnlyCollection<MepBqSourceGroup>(_sources));
        }
    }
}

public sealed class MepBqCostLine
{
    internal MepBqCostLine(MepBqProjectionLine bqLine, CostRateBuildUp rate, decimal totalCost)
    {
        ItemCode = bqLine.ItemCode;
        Description = bqLine.Description;
        Unit = bqLine.Unit;
        Quantity = bqLine.Quantity;
        RateId = rate.Id;
        Currency = rate.Currency;
        UnitRate = rate.UnitRate;
        TotalCost = totalCost;
        Sources = bqLine.Sources;
    }

    public string ItemCode { get; }
    public string Description { get; }
    public string Unit { get; }
    public decimal Quantity { get; }
    public string RateId { get; }
    public string Currency { get; }
    public decimal UnitRate { get; }
    public decimal TotalCost { get; }
    public IReadOnlyList<MepBqSourceGroup> Sources { get; }
}

public sealed class MepBqCostProjection
{
    internal MepBqCostProjection(string currency, IReadOnlyList<MepBqCostLine> lines, decimal totalCost)
    {
        Currency = currency;
        Lines = lines;
        TotalCost = totalCost;
    }

    public string Currency { get; }
    public IReadOnlyList<MepBqCostLine> Lines { get; }
    public decimal TotalCost { get; }
}

public sealed class MepBqCostProjectionService
{
    public MepBqCostProjection Price(
        IEnumerable<MepBqProjectionLine> bqLines,
        IEnumerable<CostRateBuildUp> rates,
        string currency,
        bool requireAllPriced = true)
    {
        if (bqLines is null) throw new ArgumentNullException(nameof(bqLines));
        if (rates is null) throw new ArgumentNullException(nameof(rates));
        currency = Text.Require(currency, nameof(currency));

        var rateList = new List<CostRateBuildUp>();
        var rateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rate in rates)
        {
            if (rate is null) throw new ArgumentException("MEP BQ pricing contains null rate.", nameof(rates));
            if (!rateIds.Add(rate.Id)) throw new ArgumentException("Duplicate cost rate id: " + rate.Id + ".", nameof(rates));
            rateList.Add(rate);
        }

        var result = new List<MepBqCostLine>();
        var itemCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        decimal total = 0m;
        foreach (var line in bqLines)
        {
            if (line is null) throw new ArgumentException("MEP BQ pricing contains null line.", nameof(bqLines));
            if (!itemCodes.Add(line.ItemCode)) throw new ArgumentException("Duplicate MEP BQ projection item code: " + line.ItemCode + ".", nameof(bqLines));
            var matches = rateList.Where(rate =>
                    StringComparer.OrdinalIgnoreCase.Equals(rate.ItemCode, line.ItemCode) &&
                    StringComparer.OrdinalIgnoreCase.Equals(rate.Unit, line.Unit) &&
                    StringComparer.OrdinalIgnoreCase.Equals(rate.Currency, currency))
                .OrderBy(static rate => rate.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (matches.Length == 0)
            {
                if (requireAllPriced) throw new InvalidOperationException("No exact rate matches BQ item " + line.ItemCode + " / " + line.Unit + " / " + currency + ".");
                continue;
            }
            if (matches.Length > 1)
                throw new InvalidOperationException("Multiple exact rates match BQ item " + line.ItemCode + " / " + line.Unit + " / " + currency + ": " + string.Join(", ", matches.Select(static rate => rate.Id)) + ".");
            var rate = matches[0];
            var lineTotal = checked(line.Quantity * rate.UnitRate);
            total = checked(total + lineTotal);
            result.Add(new MepBqCostLine(line, rate, lineTotal));
        }

        result.Sort(static (left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.ItemCode, right.ItemCode));
        return new MepBqCostProjection(currency, new ReadOnlyCollection<MepBqCostLine>(result), total);
    }
}

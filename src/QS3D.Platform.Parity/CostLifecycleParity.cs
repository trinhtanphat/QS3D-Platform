using System.Collections.ObjectModel;

namespace QS3D.Platform.Parity;

public sealed class CostResourceComponent
{
    public CostResourceComponent(string code, string description, string unit, decimal consumption, decimal unitCost)
    {
        Code = Text.Require(code, nameof(code));
        Description = Text.Require(description, nameof(description));
        Unit = Text.Require(unit, nameof(unit));
        if (consumption < 0m) throw new ArgumentOutOfRangeException(nameof(consumption));
        if (unitCost < 0m) throw new ArgumentOutOfRangeException(nameof(unitCost));
        Consumption = consumption;
        UnitCost = unitCost;
    }

    public string Code { get; }
    public string Description { get; }
    public string Unit { get; }
    public decimal Consumption { get; }
    public decimal UnitCost { get; }
    public decimal ExtendedCost => checked(Consumption * UnitCost);
}

public sealed class CostRateBuildUp
{
    public CostRateBuildUp(string id, string itemCode, string unit, string currency, IEnumerable<CostResourceComponent> components, decimal overheadPercent = 0m, decimal profitPercent = 0m)
    {
        Id = Text.Require(id, nameof(id));
        ItemCode = Text.Require(itemCode, nameof(itemCode));
        Unit = Text.Require(unit, nameof(unit));
        Currency = Text.Require(currency, nameof(currency));
        if (components is null) throw new ArgumentNullException(nameof(components));
        if (overheadPercent < 0m || overheadPercent > 10000m) throw new ArgumentOutOfRangeException(nameof(overheadPercent));
        if (profitPercent < 0m || profitPercent > 10000m) throw new ArgumentOutOfRangeException(nameof(profitPercent));
        OverheadPercent = overheadPercent;
        ProfitPercent = profitPercent;

        var list = new List<CostResourceComponent>();
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in components)
        {
            if (component is null) throw new ArgumentException("Rate build-up contains null component.", nameof(components));
            if (!codes.Add(component.Code)) throw new ArgumentException("Duplicate resource code: " + component.Code + ".", nameof(components));
            list.Add(component);
        }
        Components = new ReadOnlyCollection<CostResourceComponent>(list);
    }

    public string Id { get; }
    public string ItemCode { get; }
    public string Unit { get; }
    public string Currency { get; }
    public IReadOnlyList<CostResourceComponent> Components { get; }
    public decimal OverheadPercent { get; }
    public decimal ProfitPercent { get; }
    public decimal DirectUnitCost => Components.Aggregate(0m, static (sum, item) => checked(sum + item.ExtendedCost));
    public decimal OverheadUnitCost => checked(DirectUnitCost * OverheadPercent / 100m);
    public decimal ProfitBase => checked(DirectUnitCost + OverheadUnitCost);
    public decimal ProfitUnitCost => checked(ProfitBase * ProfitPercent / 100m);
    public decimal UnitRate => checked(ProfitBase + ProfitUnitCost);
}

public sealed class HistoricalCostRecord
{
    public HistoricalCostRecord(string id, string itemCode, string dimensionKey, decimal quantity, decimal totalCost, string currency, DateTime observedAtUtc)
    {
        Id = Text.Require(id, nameof(id));
        ItemCode = Text.Require(itemCode, nameof(itemCode));
        DimensionKey = Text.Require(dimensionKey, nameof(dimensionKey));
        Currency = Text.Require(currency, nameof(currency));
        if (quantity <= 0m) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (totalCost < 0m) throw new ArgumentOutOfRangeException(nameof(totalCost));
        Quantity = quantity;
        TotalCost = totalCost;
        if (observedAtUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Historical timestamp must be UTC.", nameof(observedAtUtc));
        ObservedAtUtc = observedAtUtc;
    }

    public string Id { get; }
    public string ItemCode { get; }
    public string DimensionKey { get; }
    public decimal Quantity { get; }
    public decimal TotalCost { get; }
    public string Currency { get; }
    public DateTime ObservedAtUtc { get; }
    public decimal UnitCost => TotalCost / Quantity;
}

public sealed class HistoricalCostCatalog
{
    private readonly IReadOnlyList<HistoricalCostRecord> _records;

    public HistoricalCostCatalog(IEnumerable<HistoricalCostRecord> records)
    {
        if (records is null) throw new ArgumentNullException(nameof(records));
        var list = new List<HistoricalCostRecord>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in records)
        {
            if (record is null) throw new ArgumentException("Historical catalog contains null.", nameof(records));
            if (!ids.Add(record.Id)) throw new ArgumentException("Duplicate historical record id: " + record.Id + ".", nameof(records));
            list.Add(record);
        }
        _records = new ReadOnlyCollection<HistoricalCostRecord>(list);
    }

    public IReadOnlyList<HistoricalCostRecord> Records => _records;
}

public sealed class CostBenchmarkResult
{
    internal CostBenchmarkResult(int sampleCount, decimal minimum, decimal maximum, decimal average, decimal median, decimal? candidate, decimal? deviationPercent)
    {
        SampleCount = sampleCount;
        MinimumUnitCost = minimum;
        MaximumUnitCost = maximum;
        AverageUnitCost = average;
        MedianUnitCost = median;
        CandidateUnitCost = candidate;
        DeviationFromAveragePercent = deviationPercent;
    }

    public int SampleCount { get; }
    public decimal MinimumUnitCost { get; }
    public decimal MaximumUnitCost { get; }
    public decimal AverageUnitCost { get; }
    public decimal MedianUnitCost { get; }
    public decimal? CandidateUnitCost { get; }
    public decimal? DeviationFromAveragePercent { get; }
}

public sealed class CostBenchmarkService
{
    public CostBenchmarkResult Analyze(HistoricalCostCatalog catalog, string itemCode, string dimensionKey, string currency, decimal? candidateUnitCost = null)
    {
        if (catalog is null) throw new ArgumentNullException(nameof(catalog));
        itemCode = Text.Require(itemCode, nameof(itemCode));
        dimensionKey = Text.Require(dimensionKey, nameof(dimensionKey));
        currency = Text.Require(currency, nameof(currency));
        if (candidateUnitCost.HasValue && candidateUnitCost.Value < 0m) throw new ArgumentOutOfRangeException(nameof(candidateUnitCost));
        var values = catalog.Records
            .Where(x => StringComparer.OrdinalIgnoreCase.Equals(x.ItemCode, itemCode) &&
                        StringComparer.OrdinalIgnoreCase.Equals(x.DimensionKey, dimensionKey) &&
                        StringComparer.OrdinalIgnoreCase.Equals(x.Currency, currency))
            .Select(static x => x.UnitCost)
            .OrderBy(static x => x)
            .ToArray();
        if (values.Length == 0) throw new InvalidOperationException("No comparable historical samples were found.");
        var sum = values.Aggregate(0m, static (total, value) => checked(total + value));
        var average = sum / values.Length;
        var median = values.Length % 2 == 1
            ? values[values.Length / 2]
            : (values[values.Length / 2 - 1] + values[values.Length / 2]) / 2m;
        decimal? deviation = null;
        if (candidateUnitCost.HasValue)
            deviation = average == 0m ? (candidateUnitCost.Value == 0m ? 0m : null) : (candidateUnitCost.Value - average) / average * 100m;
        return new CostBenchmarkResult(values.Length, values[0], values[values.Length - 1], average, median, candidateUnitCost, deviation);
    }
}

public sealed class BqLibraryItem
{
    public BqLibraryItem(string itemCode, string description, string unit, string categoryPath = "Unclassified")
    {
        ItemCode = Text.Require(itemCode, nameof(itemCode));
        Description = Text.Require(description, nameof(description));
        Unit = Text.Require(unit, nameof(unit));
        CategoryPath = Text.Require(categoryPath, nameof(categoryPath));
    }

    public string ItemCode { get; }
    public string Description { get; }
    public string Unit { get; }
    public string CategoryPath { get; }
}

public sealed class BqLibraryCatalog
{
    private readonly Dictionary<string, BqLibraryItem> _items = new(StringComparer.OrdinalIgnoreCase);

    public BqLibraryCatalog(IEnumerable<BqLibraryItem>? items = null)
    {
        if (items is null) return;
        ImportFromProject(items, false);
    }

    public IReadOnlyList<BqLibraryItem> Items => new ReadOnlyCollection<BqLibraryItem>(_items.Values.OrderBy(static x => x.ItemCode, StringComparer.OrdinalIgnoreCase).ToList());

    public void ImportFromProject(IEnumerable<BqLibraryItem> projectEntries, bool replaceExisting)
    {
        if (projectEntries is null) throw new ArgumentNullException(nameof(projectEntries));
        var incoming = new List<BqLibraryItem>();
        var incomingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in projectEntries)
        {
            if (item is null) throw new ArgumentException("BQ payload contains null.", nameof(projectEntries));
            if (!incomingCodes.Add(item.ItemCode)) throw new ArgumentException("Duplicate incoming BQ item code: " + item.ItemCode + ".", nameof(projectEntries));
            if (!replaceExisting && _items.ContainsKey(item.ItemCode)) throw new InvalidOperationException("BQ item already exists: " + item.ItemCode + ".");
            incoming.Add(item);
        }
        foreach (var item in incoming) _items[item.ItemCode] = item;
    }

    public BqLibraryItem? Find(string itemCode)
    {
        itemCode = Text.Require(itemCode, nameof(itemCode));
        return _items.TryGetValue(itemCode, out var item) ? item : null;
    }
}

public sealed class CostReferenceMark
{
    public CostReferenceMark(string markId, string bqItemCode, string rateId, string sourceLabel)
    {
        MarkId = Text.Require(markId, nameof(markId));
        BqItemCode = Text.Require(bqItemCode, nameof(bqItemCode));
        RateId = Text.Require(rateId, nameof(rateId));
        SourceLabel = Text.Require(sourceLabel, nameof(sourceLabel));
    }

    public string MarkId { get; }
    public string BqItemCode { get; }
    public string RateId { get; }
    public string SourceLabel { get; }
}

public sealed class CostReferenceIndex
{
    private readonly IReadOnlyList<CostReferenceMark> _marks;

    public CostReferenceIndex(IEnumerable<CostReferenceMark> marks)
    {
        if (marks is null) throw new ArgumentNullException(nameof(marks));
        var list = new List<CostReferenceMark>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mark in marks)
        {
            if (mark is null) throw new ArgumentException("Reference index contains null.", nameof(marks));
            if (!ids.Add(mark.MarkId)) throw new ArgumentException("Duplicate reference mark id: " + mark.MarkId + ".", nameof(marks));
            list.Add(mark);
        }
        _marks = new ReadOnlyCollection<CostReferenceMark>(list.OrderBy(static x => x.MarkId, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public IReadOnlyList<CostReferenceMark> FindByBqItem(string itemCode) => Find(itemCode, static (mark, value) => StringComparer.OrdinalIgnoreCase.Equals(mark.BqItemCode, value));
    public IReadOnlyList<CostReferenceMark> FindByRate(string rateId) => Find(rateId, static (mark, value) => StringComparer.OrdinalIgnoreCase.Equals(mark.RateId, value));

    private IReadOnlyList<CostReferenceMark> Find(string value, Func<CostReferenceMark, string, bool> predicate)
    {
        value = Text.Require(value, nameof(value));
        return new ReadOnlyCollection<CostReferenceMark>(_marks.Where(x => predicate(x, value)).ToList());
    }
}

public sealed class CostAdjustmentResult
{
    internal CostAdjustmentResult(decimal originalTotal, decimal adjustedTotal, decimal delta, decimal ratioPercent)
    {
        OriginalTotal = originalTotal;
        AdjustedTotal = adjustedTotal;
        Delta = delta;
        RatioPercent = ratioPercent;
    }

    public decimal OriginalTotal { get; }
    public decimal AdjustedTotal { get; }
    public decimal Delta { get; }
    public decimal RatioPercent { get; }
}

public static class CostAdjustmentService
{
    public static CostAdjustmentResult ByRatio(decimal originalTotal, decimal ratioPercent)
    {
        if (originalTotal < 0m) throw new ArgumentOutOfRangeException(nameof(originalTotal));
        if (ratioPercent < -100m) throw new ArgumentOutOfRangeException(nameof(ratioPercent));
        var adjusted = checked(originalTotal * (100m + ratioPercent) / 100m);
        return new CostAdjustmentResult(originalTotal, adjusted, adjusted - originalTotal, ratioPercent);
    }

    public static CostAdjustmentResult ToTarget(decimal originalTotal, decimal targetTotal)
    {
        if (originalTotal < 0m) throw new ArgumentOutOfRangeException(nameof(originalTotal));
        if (targetTotal < 0m) throw new ArgumentOutOfRangeException(nameof(targetTotal));
        if (originalTotal == 0m && targetTotal > 0m)
            throw new InvalidOperationException("A positive target total cannot be represented by a finite adjustment ratio when the original total is zero.");
        var ratio = originalTotal == 0m ? 0m : (targetTotal - originalTotal) / originalTotal * 100m;
        return new CostAdjustmentResult(originalTotal, targetTotal, targetTotal - originalTotal, ratio);
    }
}

public sealed class TradeCostLine
{
    public TradeCostLine(string tradeCode, decimal cost)
    {
        TradeCode = string.IsNullOrWhiteSpace(tradeCode) ? "Unclassified" : tradeCode.Trim();
        if (cost < 0m) throw new ArgumentOutOfRangeException(nameof(cost));
        Cost = cost;
    }

    public string TradeCode { get; }
    public decimal Cost { get; }
}

public sealed class TradeCostSummary
{
    internal TradeCostSummary(string tradeCode, decimal cost, decimal totalCost, decimal floorAreaM2)
    {
        TradeCode = tradeCode;
        Cost = cost;
        SharePercent = totalCost == 0m ? 0m : cost / totalCost * 100m;
        CostPerM2 = floorAreaM2 == 0m ? null : cost / floorAreaM2;
    }

    public string TradeCode { get; }
    public decimal Cost { get; }
    public decimal SharePercent { get; }
    public decimal? CostPerM2 { get; }
}

public static class TradeCostAnalysisService
{
    public static IReadOnlyList<TradeCostSummary> Analyze(IEnumerable<TradeCostLine> lines, decimal floorAreaM2)
    {
        if (lines is null) throw new ArgumentNullException(nameof(lines));
        if (floorAreaM2 < 0m) throw new ArgumentOutOfRangeException(nameof(floorAreaM2));
        var grouped = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            if (line is null) throw new ArgumentException("Trade analysis contains null.", nameof(lines));
            grouped[line.TradeCode] = checked(grouped.TryGetValue(line.TradeCode, out var existing) ? existing + line.Cost : line.Cost);
        }
        var total = grouped.Values.Aggregate(0m, static (sum, value) => checked(sum + value));
        var result = grouped.Select(x => new TradeCostSummary(x.Key, x.Value, total, floorAreaM2))
            .OrderBy(static x => x.TradeCode, StringComparer.OrdinalIgnoreCase).ToList();
        return new ReadOnlyCollection<TradeCostSummary>(result);
    }
}

public sealed class TenderRequirement
{
    public TenderRequirement(string itemCode, string description, string unit, decimal quantity)
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

public sealed class TenderQuoteLine
{
    public TenderQuoteLine(string itemCode, decimal unitRate)
    {
        ItemCode = Text.Require(itemCode, nameof(itemCode));
        if (unitRate < 0m) throw new ArgumentOutOfRangeException(nameof(unitRate));
        UnitRate = unitRate;
    }
    public string ItemCode { get; }
    public decimal UnitRate { get; }
}

public sealed class TenderBid
{
    public TenderBid(string bidId, string bidder, string currency, IEnumerable<TenderQuoteLine> lines)
    {
        BidId = Text.Require(bidId, nameof(bidId));
        Bidder = Text.Require(bidder, nameof(bidder));
        Currency = Text.Require(currency, nameof(currency));
        if (lines is null) throw new ArgumentNullException(nameof(lines));
        var list = new List<TenderQuoteLine>();
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            if (line is null) throw new ArgumentException("Tender bid contains null line.", nameof(lines));
            if (!codes.Add(line.ItemCode)) throw new ArgumentException("Duplicate tender item code: " + line.ItemCode + ".", nameof(lines));
            list.Add(line);
        }
        Lines = new ReadOnlyCollection<TenderQuoteLine>(list);
    }
    public string BidId { get; }
    public string Bidder { get; }
    public string Currency { get; }
    public IReadOnlyList<TenderQuoteLine> Lines { get; }
}

public sealed class TenderEvaluationResult
{
    internal TenderEvaluationResult(string bidId, string bidder, string currency, decimal evaluatedTotal, IReadOnlyList<string> missingItemCodes)
    {
        BidId = bidId;
        Bidder = bidder;
        Currency = currency;
        EvaluatedTotal = evaluatedTotal;
        MissingItemCodes = missingItemCodes;
    }
    public string BidId { get; }
    public string Bidder { get; }
    public string Currency { get; }
    public decimal EvaluatedTotal { get; }
    public IReadOnlyList<string> MissingItemCodes { get; }
    public bool IsComplete => MissingItemCodes.Count == 0;
    public int Rank { get; internal set; }
}

public sealed class TenderEvaluationService
{
    public IReadOnlyList<TenderEvaluationResult> Evaluate(IEnumerable<TenderRequirement> requirements, IEnumerable<TenderBid> bids)
    {
        if (requirements is null) throw new ArgumentNullException(nameof(requirements));
        if (bids is null) throw new ArgumentNullException(nameof(bids));
        var required = new List<TenderRequirement>();
        var requiredCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in requirements)
        {
            if (item is null) throw new ArgumentException("Tender requirements contain null.", nameof(requirements));
            if (!requiredCodes.Add(item.ItemCode)) throw new ArgumentException("Duplicate tender requirement: " + item.ItemCode + ".", nameof(requirements));
            required.Add(item);
        }

        var results = new List<TenderEvaluationResult>();
        var bidIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? commonCurrency = null;
        foreach (var bid in bids)
        {
            if (bid is null) throw new ArgumentException("Tender bids contain null.", nameof(bids));
            if (!bidIds.Add(bid.BidId)) throw new ArgumentException("Duplicate tender bid id: " + bid.BidId + ".", nameof(bids));
            commonCurrency ??= bid.Currency;
            if (!StringComparer.OrdinalIgnoreCase.Equals(commonCurrency, bid.Currency)) throw new InvalidOperationException("Tender bids must use one comparable currency.");
            var byCode = bid.Lines.ToDictionary(static x => x.ItemCode, StringComparer.OrdinalIgnoreCase);
            var missing = new List<string>();
            var total = 0m;
            foreach (var requirement in required)
            {
                if (!byCode.TryGetValue(requirement.ItemCode, out var quote))
                {
                    missing.Add(requirement.ItemCode);
                    continue;
                }
                total = checked(total + requirement.Quantity * quote.UnitRate);
            }
            missing.Sort(StringComparer.OrdinalIgnoreCase);
            results.Add(new TenderEvaluationResult(bid.BidId, bid.Bidder, bid.Currency, total, new ReadOnlyCollection<string>(missing)));
        }

        var rank = 1;
        foreach (var result in results.Where(static x => x.IsComplete).OrderBy(static x => x.EvaluatedTotal).ThenBy(static x => x.BidId, StringComparer.OrdinalIgnoreCase))
            result.Rank = rank++;
        return new ReadOnlyCollection<TenderEvaluationResult>(results);
    }
}

public sealed class ProgressContractItem
{
    public ProgressContractItem(string itemCode, string unit, decimal contractQuantity, decimal unitRate)
    {
        ItemCode = Text.Require(itemCode, nameof(itemCode));
        Unit = Text.Require(unit, nameof(unit));
        if (contractQuantity < 0m) throw new ArgumentOutOfRangeException(nameof(contractQuantity));
        if (unitRate < 0m) throw new ArgumentOutOfRangeException(nameof(unitRate));
        ContractQuantity = contractQuantity;
        UnitRate = unitRate;
    }
    public string ItemCode { get; }
    public string Unit { get; }
    public decimal ContractQuantity { get; }
    public decimal UnitRate { get; }
}

public sealed class ProgressClaimLine
{
    public ProgressClaimLine(string itemCode, decimal previouslyCertifiedQuantity, decimal claimedThisPeriodQuantity)
    {
        ItemCode = Text.Require(itemCode, nameof(itemCode));
        if (previouslyCertifiedQuantity < 0m) throw new ArgumentOutOfRangeException(nameof(previouslyCertifiedQuantity));
        if (claimedThisPeriodQuantity < 0m) throw new ArgumentOutOfRangeException(nameof(claimedThisPeriodQuantity));
        PreviouslyCertifiedQuantity = previouslyCertifiedQuantity;
        ClaimedThisPeriodQuantity = claimedThisPeriodQuantity;
    }
    public string ItemCode { get; }
    public decimal PreviouslyCertifiedQuantity { get; }
    public decimal ClaimedThisPeriodQuantity { get; }
}

public sealed class ProgressClaimEvaluationLine
{
    internal ProgressClaimEvaluationLine(string itemCode, decimal certifiedQuantity, decimal rejectedQuantity, decimal certifiedValue)
    {
        ItemCode = itemCode;
        CertifiedThisPeriodQuantity = certifiedQuantity;
        RejectedQuantity = rejectedQuantity;
        CertifiedValue = certifiedValue;
    }
    public string ItemCode { get; }
    public decimal CertifiedThisPeriodQuantity { get; }
    public decimal RejectedQuantity { get; }
    public decimal CertifiedValue { get; }
}

public sealed class ProgressClaimResult
{
    internal ProgressClaimResult(IReadOnlyList<ProgressClaimEvaluationLine> lines, decimal gross, decimal retention, decimal net)
    {
        Lines = lines;
        GrossCertifiedThisPeriod = gross;
        RetentionThisPeriod = retention;
        NetCertifiedThisPeriod = net;
    }
    public IReadOnlyList<ProgressClaimEvaluationLine> Lines { get; }
    public decimal GrossCertifiedThisPeriod { get; }
    public decimal RetentionThisPeriod { get; }
    public decimal NetCertifiedThisPeriod { get; }
}

public sealed class ProgressClaimService
{
    public ProgressClaimResult Evaluate(IEnumerable<ProgressContractItem> contractItems, IEnumerable<ProgressClaimLine> claims, decimal retentionPercent)
    {
        if (contractItems is null) throw new ArgumentNullException(nameof(contractItems));
        if (claims is null) throw new ArgumentNullException(nameof(claims));
        if (retentionPercent < 0m || retentionPercent > 100m) throw new ArgumentOutOfRangeException(nameof(retentionPercent));
        var contracts = contractItems.ToDictionary(static x => x.ItemCode, StringComparer.OrdinalIgnoreCase);
        var seenClaims = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = new List<ProgressClaimEvaluationLine>();
        var gross = 0m;
        foreach (var claim in claims)
        {
            if (claim is null) throw new ArgumentException("Progress claims contain null.", nameof(claims));
            if (!seenClaims.Add(claim.ItemCode)) throw new ArgumentException("Duplicate progress claim item: " + claim.ItemCode + ".", nameof(claims));
            if (!contracts.TryGetValue(claim.ItemCode, out var contract)) throw new InvalidOperationException("Progress claim references unknown contract item: " + claim.ItemCode + ".");
            if (claim.PreviouslyCertifiedQuantity > contract.ContractQuantity) throw new InvalidOperationException("Previously certified quantity exceeds contract quantity for " + claim.ItemCode + ".");
            var remaining = contract.ContractQuantity - claim.PreviouslyCertifiedQuantity;
            var certified = Math.Min(remaining, claim.ClaimedThisPeriodQuantity);
            var rejected = claim.ClaimedThisPeriodQuantity - certified;
            var value = checked(certified * contract.UnitRate);
            gross = checked(gross + value);
            lines.Add(new ProgressClaimEvaluationLine(claim.ItemCode, certified, rejected, value));
        }
        lines.Sort(static (a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.ItemCode, b.ItemCode));
        var retention = checked(gross * retentionPercent / 100m);
        return new ProgressClaimResult(new ReadOnlyCollection<ProgressClaimEvaluationLine>(lines), gross, retention, gross - retention);
    }
}

public sealed class TimePhasedCostItem
{
    public TimePhasedCostItem(string itemId, DateTime periodStartUtc, decimal baselineValue, decimal actualValue, decimal certifiedValue)
    {
        ItemId = Text.Require(itemId, nameof(itemId));
        if (periodStartUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Period timestamp must be UTC.", nameof(periodStartUtc));
        if (baselineValue < 0m || actualValue < 0m || certifiedValue < 0m) throw new ArgumentOutOfRangeException(nameof(baselineValue), "Time-phased values must be non-negative.");
        PeriodStartUtc = periodStartUtc;
        BaselineValue = baselineValue;
        ActualValue = actualValue;
        CertifiedValue = certifiedValue;
    }
    public string ItemId { get; }
    public DateTime PeriodStartUtc { get; }
    public decimal BaselineValue { get; }
    public decimal ActualValue { get; }
    public decimal CertifiedValue { get; }
}

public sealed class TimePhasedCostBucket
{
    internal TimePhasedCostBucket(DateTime periodStartUtc, decimal baselineValue, decimal actualValue, decimal certifiedValue, decimal cumulativeBaseline, decimal cumulativeActual, decimal cumulativeCertified)
    {
        PeriodStartUtc = periodStartUtc;
        BaselineValue = baselineValue;
        ActualValue = actualValue;
        CertifiedValue = certifiedValue;
        CumulativeBaselineValue = cumulativeBaseline;
        CumulativeActualValue = cumulativeActual;
        CumulativeCertifiedValue = cumulativeCertified;
    }
    public DateTime PeriodStartUtc { get; }
    public decimal BaselineValue { get; }
    public decimal ActualValue { get; }
    public decimal CertifiedValue { get; }
    public decimal CumulativeBaselineValue { get; }
    public decimal CumulativeActualValue { get; }
    public decimal CumulativeCertifiedValue { get; }
}

public static class TimePhasedCostService
{
    public static IReadOnlyList<TimePhasedCostBucket> Summarize(IEnumerable<TimePhasedCostItem> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        var groups = new SortedDictionary<DateTime, decimal[]>();
        foreach (var item in items)
        {
            if (item is null) throw new ArgumentException("Time-phased input contains null.", nameof(items));
            if (!groups.TryGetValue(item.PeriodStartUtc, out var values))
            {
                values = new decimal[3];
                groups.Add(item.PeriodStartUtc, values);
            }
            values[0] = checked(values[0] + item.BaselineValue);
            values[1] = checked(values[1] + item.ActualValue);
            values[2] = checked(values[2] + item.CertifiedValue);
        }
        var result = new List<TimePhasedCostBucket>();
        var baseline = 0m;
        var actual = 0m;
        var certified = 0m;
        foreach (var pair in groups)
        {
            baseline = checked(baseline + pair.Value[0]);
            actual = checked(actual + pair.Value[1]);
            certified = checked(certified + pair.Value[2]);
            result.Add(new TimePhasedCostBucket(pair.Key, pair.Value[0], pair.Value[1], pair.Value[2], baseline, actual, certified));
        }
        return new ReadOnlyCollection<TimePhasedCostBucket>(result);
    }
}
using System.Collections;
using System.Globalization;

namespace QS3D.Platform.Quantity;

public readonly struct Money : IEquatable<Money>
{
    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency must not be blank.", nameof(currency));
        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(static c => c < 'A' || c > 'Z'))
            throw new ArgumentException("Currency must be a three-letter uppercase-compatible code.", nameof(currency));
        Amount = amount;
        Currency = normalized;
    }

    public decimal Amount { get; }
    public string Currency { get; }
    public bool Equals(Money other) => Amount == other.Amount && StringComparer.Ordinal.Equals(Currency, other.Currency);
    public override bool Equals(object? obj) => obj is Money other && Equals(other);
    public override int GetHashCode()
    {
        unchecked { return (Amount.GetHashCode() * 397) ^ StringComparer.Ordinal.GetHashCode(Currency); }
    }
    public static bool operator ==(Money left, Money right) => left.Equals(right);
    public static bool operator !=(Money left, Money right) => !left.Equals(right);
    public override string ToString() => Amount.ToString(CultureInfo.InvariantCulture) + " " + Currency;
}

public sealed class UnitRate
{
    public UnitRate(string quantityCode, QuantityDimension dimension, decimal amountPerCanonicalUnit, string currency)
    {
        if (string.IsNullOrWhiteSpace(quantityCode)) throw new ArgumentException("Quantity code must not be blank.", nameof(quantityCode));
        if (!Enum.IsDefined(typeof(QuantityDimension), dimension)) throw new ArgumentOutOfRangeException(nameof(dimension));
        if (amountPerCanonicalUnit < 0m) throw new ArgumentOutOfRangeException(nameof(amountPerCanonicalUnit));
        QuantityCode = quantityCode.Trim();
        Dimension = dimension;
        AmountPerCanonicalUnit = amountPerCanonicalUnit;
        Currency = new Money(0m, currency).Currency;
    }

    public string QuantityCode { get; }
    public QuantityDimension Dimension { get; }
    public decimal AmountPerCanonicalUnit { get; }
    public string Currency { get; }
}

public sealed class BoqLine
{
    public BoqLine(string code, QuantityValue quantity, int elementCount, decimal unitRate, Money total)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("BQ code must not be blank.", nameof(code));
        if (elementCount < 0) throw new ArgumentOutOfRangeException(nameof(elementCount));
        if (elementCount == 0 && quantity.Value != 0d)
            throw new ArgumentException("A positive BQ quantity requires at least one contributing element.", nameof(elementCount));
        if (unitRate < 0m) throw new ArgumentOutOfRangeException(nameof(unitRate));
        Code = code.Trim();
        Quantity = quantity;
        ElementCount = elementCount;
        UnitRate = unitRate;
        Total = total;
    }

    public string Code { get; }
    public QuantityValue Quantity { get; }
    public int ElementCount { get; }
    public decimal UnitRate { get; }
    public Money Total { get; }
}

public sealed class BoqProjection
{
    public BoqProjection(IEnumerable<BoqLine> lines, string currency)
    {
        if (lines is null) throw new ArgumentNullException(nameof(lines));
        Currency = new Money(0m, currency).Currency;
        var copiedLines = BoqInputMaterializer.Materialize(lines, nameof(lines), "BQ lines");
        if (copiedLines.Any(static line => line is null))
            throw new ArgumentException("BQ lines must not contain null entries.", nameof(lines));
        EnsureUniqueLineKeys(copiedLines);
        var orderedLines = copiedLines.OrderBy(static x => x.Code, StringComparer.Ordinal)
            .ThenBy(static x => x.Quantity.Dimension)
            .ToArray();
        Lines = Array.AsReadOnly(orderedLines);

        foreach (var line in Lines)
        {
            if (!StringComparer.Ordinal.Equals(line.Total.Currency, Currency))
                throw new InvalidOperationException("All BQ lines must use the projection currency.");
            var expectedTotal = BoqArithmetic.CalculateTotal(line.Code, line.Quantity, line.UnitRate);
            if (line.Total.Amount != expectedTotal)
                throw new InvalidOperationException($"BQ line total mismatch for '{line.Code}'/{line.Quantity.Dimension}: expected {expectedTotal.ToString(CultureInfo.InvariantCulture)}, got {line.Total.Amount.ToString(CultureInfo.InvariantCulture)}.");
        }

        Total = new Money(Lines.Sum(static line => line.Total.Amount), Currency);
    }

    public string Currency { get; }
    public IReadOnlyList<BoqLine> Lines { get; }
    public Money Total { get; }

    private static void EnsureUniqueLineKeys(IEnumerable<BoqLine> lines)
    {
        var dimensionsByCode = new Dictionary<string, HashSet<QuantityDimension>>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            if (!dimensionsByCode.TryGetValue(line.Code, out var dimensions))
            {
                dimensions = new HashSet<QuantityDimension>();
                dimensionsByCode.Add(line.Code, dimensions);
            }

            if (!dimensions.Add(line.Quantity.Dimension))
                throw new InvalidOperationException($"Duplicate BQ line for '{line.Code}'/{line.Quantity.Dimension}.");
        }
    }
}

public static class BoqProjector
{
    public static BoqProjection Project(
        IEnumerable<QuantitySummary> quantities,
        IEnumerable<UnitRate> rates,
        string currency,
        bool requireRateForEveryQuantity = true)
    {
        if (quantities is null) throw new ArgumentNullException(nameof(quantities));
        if (rates is null) throw new ArgumentNullException(nameof(rates));
        var normalizedCurrency = new Money(0m, currency).Currency;
        var copiedRates = BoqInputMaterializer.Materialize(rates, nameof(rates), "BQ unit rates");
        if (copiedRates.Any(static rate => rate is null))
            throw new ArgumentException("BQ unit rates must not contain null entries.", nameof(rates));
        var copiedQuantities = BoqInputMaterializer.Materialize(quantities, nameof(quantities), "BQ quantity summaries");
        if (copiedQuantities.Any(static quantity => quantity is null))
            throw new ArgumentException("BQ quantity summaries must not contain null entries.", nameof(quantities));

        var rateMap = new Dictionary<RateKey, UnitRate>(RateKeyComparer.Instance);
        foreach (var rate in copiedRates)
        {
            if (!StringComparer.Ordinal.Equals(rate.Currency, normalizedCurrency))
                throw new InvalidOperationException($"Rate '{rate.QuantityCode}' uses {rate.Currency}, expected {normalizedCurrency}.");
            var key = new RateKey(rate.QuantityCode, rate.Dimension);
            if (rateMap.ContainsKey(key))
                throw new InvalidOperationException($"Duplicate unit rate for '{rate.QuantityCode}'/{rate.Dimension}.");
            rateMap.Add(key, rate);
        }

        var lines = new List<BoqLine>();
        var quantityKeys = new HashSet<RateKey>(RateKeyComparer.Instance);
        foreach (var quantity in copiedQuantities)
        {
            var key = new RateKey(quantity.Code, quantity.Quantity.Dimension);
            if (!quantityKeys.Add(key))
                throw new InvalidOperationException($"Duplicate quantity summary for '{quantity.Code}'/{quantity.Quantity.Dimension}.");
            if (!rateMap.TryGetValue(key, out var rate))
            {
                if (requireRateForEveryQuantity)
                    throw new InvalidOperationException($"Missing unit rate for '{quantity.Code}'/{quantity.Quantity.Dimension}.");
                continue;
            }

            var total = BoqArithmetic.CalculateTotal(quantity.Code, quantity.Quantity, rate.AmountPerCanonicalUnit);
            lines.Add(new BoqLine(
                quantity.Code,
                quantity.Quantity,
                quantity.ElementCount,
                rate.AmountPerCanonicalUnit,
                new Money(total, normalizedCurrency)));
        }

        return new BoqProjection(lines, normalizedCurrency);
    }

    private readonly struct RateKey
    {
        public RateKey(string code, QuantityDimension dimension) { Code = code; Dimension = dimension; }
        public string Code { get; }
        public QuantityDimension Dimension { get; }
    }

    private sealed class RateKeyComparer : IEqualityComparer<RateKey>
    {
        public static readonly RateKeyComparer Instance = new();
        public bool Equals(RateKey x, RateKey y) => x.Dimension == y.Dimension && StringComparer.Ordinal.Equals(x.Code, y.Code);
        public int GetHashCode(RateKey obj)
        {
            unchecked { return (StringComparer.Ordinal.GetHashCode(obj.Code) * 397) ^ (int)obj.Dimension; }
        }
    }
}

internal static class BoqInputMaterializer
{
    internal const int MaximumEntries = 100_000;

    internal static T[] Materialize<T>(IEnumerable<T> source, string parameterName, string entryDescription)
    {
        if (source is null) throw new ArgumentNullException(parameterName);
        if (string.IsNullOrWhiteSpace(entryDescription)) throw new ArgumentException("Entry description must not be blank.", nameof(entryDescription));

        var advertisedCount = CaptureCurrentCount(source, parameterName, entryDescription);
        var result = advertisedCount.HasValue ? new List<T>(advertisedCount.Value) : new List<T>();
        foreach (var item in source)
        {
            if (result.Count >= MaximumEntries)
                throw new InvalidOperationException($"{entryDescription} exceed the supported maximum of {MaximumEntries} entries.");
            result.Add(item);
        }

        if (advertisedCount.HasValue && advertisedCount.Value != result.Count)
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");

        var finalCount = CaptureCurrentCount(source, parameterName, entryDescription);
        if (advertisedCount.HasValue != finalCount.HasValue
            || (advertisedCount.HasValue && advertisedCount.Value != finalCount!.Value))
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");

        return result.ToArray();
    }

    private static int? CaptureCurrentCount<T>(IEnumerable<T> source, string parameterName, string entryDescription)
    {
        int? count = null;
        CaptureCount(source as ICollection<T>, static collection => collection.Count, ref count, parameterName, entryDescription);
        CaptureCount(source as IReadOnlyCollection<T>, static collection => collection.Count, ref count, parameterName, entryDescription);
        CaptureCount(source as ICollection, static collection => collection.Count, ref count, parameterName, entryDescription);
        return count;
    }

    private static void CaptureCount<TCollection>(
        TCollection? collection,
        Func<TCollection, int> getCount,
        ref int? advertisedCount,
        string parameterName,
        string entryDescription)
        where TCollection : class
    {
        if (collection is null) return;
        var count = getCount(collection);
        if (count < 0)
            throw new ArgumentException($"{entryDescription} reported a negative Count.", parameterName);
        if (count > MaximumEntries)
            throw new InvalidOperationException($"{entryDescription} exceed the supported maximum of {MaximumEntries} entries.");
        if (advertisedCount.HasValue && advertisedCount.Value != count)
            throw new InvalidOperationException($"{entryDescription} expose conflicting Count values.");
        advertisedCount = count;
    }
}

internal static class BoqArithmetic
{
    public static decimal CalculateTotal(string code, QuantityValue quantity, decimal unitRate)
    {
        var canonicalQuantity = ConvertCanonicalQuantityToDecimal(code, quantity);
        try { return checked(canonicalQuantity * unitRate); }
        catch (OverflowException ex)
        {
            throw new OverflowException($"Cost for '{code}' exceeds decimal range.", ex);
        }
    }

    private static decimal ConvertCanonicalQuantityToDecimal(string code, QuantityValue quantity)
    {
        var source = quantity.Value;
        var roundTripText = source.ToString("R", CultureInfo.InvariantCulture);
        if (!decimal.TryParse(
                roundTripText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var canonicalQuantity)
            || (double)canonicalQuantity != source)
        {
            throw new OverflowException($"Quantity '{code}' cannot be represented as decimal for cost projection.");
        }

        return canonicalQuantity;
    }
}

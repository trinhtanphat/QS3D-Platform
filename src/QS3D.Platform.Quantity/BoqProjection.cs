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
        Lines = lines.OrderBy(static x => x.Code, StringComparer.Ordinal)
            .ThenBy(static x => x.Quantity.Dimension)
            .ToArray();
        if (Lines.Any(line => !StringComparer.Ordinal.Equals(line.Total.Currency, Currency)))
            throw new InvalidOperationException("All BQ lines must use the projection currency.");
        Total = new Money(Lines.Sum(static line => line.Total.Amount), Currency);
    }

    public string Currency { get; }
    public IReadOnlyList<BoqLine> Lines { get; }
    public Money Total { get; }
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

        var rateMap = new Dictionary<RateKey, UnitRate>(RateKeyComparer.Instance);
        foreach (var rate in rates)
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
        foreach (var quantity in quantities)
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

            var canonicalQuantity = ConvertCanonicalQuantityToDecimal(quantity);

            decimal total;
            try { total = checked(canonicalQuantity * rate.AmountPerCanonicalUnit); }
            catch (OverflowException ex)
            {
                throw new OverflowException($"Cost for '{quantity.Code}' exceeds decimal range.", ex);
            }

            lines.Add(new BoqLine(
                quantity.Code,
                quantity.Quantity,
                quantity.ElementCount,
                rate.AmountPerCanonicalUnit,
                new Money(total, normalizedCurrency)));
        }

        return new BoqProjection(lines, normalizedCurrency);
    }

    private static decimal ConvertCanonicalQuantityToDecimal(QuantitySummary quantity)
    {
        var source = quantity.Quantity.Value;
        var roundTripText = source.ToString("R", CultureInfo.InvariantCulture);
        if (!decimal.TryParse(
                roundTripText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var canonicalQuantity)
            || (double)canonicalQuantity != source)
        {
            throw new OverflowException($"Quantity '{quantity.Code}' cannot be represented as decimal for cost projection.");
        }

        return canonicalQuantity;
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

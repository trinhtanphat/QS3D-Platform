using System.Globalization;
using QS3D.Platform.Domain;
using QS3D.Platform.Geometry;

namespace QS3D.Platform.Quantity;

public enum QuantityDimension
{
    Count = 0,
    Length,
    Area,
    Volume,
    Mass
}

public readonly struct QuantityValue : IEquatable<QuantityValue>
{
    public QuantityValue(QuantityDimension dimension, double value)
    {
        if (!Enum.IsDefined(typeof(QuantityDimension), dimension)) throw new ArgumentOutOfRangeException(nameof(dimension));
        Dimension = dimension;
        Value = Numeric.RequireNonNegativeFinite(value, nameof(value));
    }

    public QuantityDimension Dimension { get; }
    public double Value { get; }

    public string CanonicalUnit
    {
        get
        {
            switch (Dimension)
            {
                case QuantityDimension.Count: return "ea";
                case QuantityDimension.Length: return "m";
                case QuantityDimension.Area: return "m2";
                case QuantityDimension.Volume: return "m3";
                case QuantityDimension.Mass: return "kg";
                default: throw new ArgumentOutOfRangeException(nameof(Dimension), Dimension, "Unsupported quantity dimension.");
            }
        }
    }

    public bool Equals(QuantityValue other) => Dimension == other.Dimension && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is QuantityValue other && Equals(other);
    public override int GetHashCode()
    {
        unchecked { return ((int)Dimension * 397) ^ Value.GetHashCode(); }
    }
    public override string ToString() => Value.ToString("R", CultureInfo.InvariantCulture) + " " + CanonicalUnit;
}

public sealed class QuantityFact
{
    public QuantityFact(ElementId elementId, string code, QuantityValue quantity, CadReference? sourceReference = null)
    {
        if (elementId.Value == Guid.Empty) throw new ArgumentException("Element ID must not be empty.", nameof(elementId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Quantity code must not be blank.", nameof(code));
        if (sourceReference.HasValue)
        {
            if (sourceReference.Value.DrawingId.Value == Guid.Empty)
                throw new ArgumentException("Quantity source drawing ID must not be empty.", nameof(sourceReference));
            if (string.IsNullOrWhiteSpace(sourceReference.Value.Handle.Value))
                throw new ArgumentException("Quantity source CAD handle must not be empty.", nameof(sourceReference));
        }
        ElementId = elementId;
        Code = code.Trim();
        Quantity = quantity;
        SourceReference = sourceReference;
    }

    public ElementId ElementId { get; }
    public string Code { get; }
    public QuantityValue Quantity { get; }
    public CadReference? SourceReference { get; }
}

public sealed class QuantitySummary
{
    public QuantitySummary(string code, QuantityDimension dimension, double value, int factCount, int elementCount)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Quantity code must not be blank.", nameof(code));
        if (factCount < 0) throw new ArgumentOutOfRangeException(nameof(factCount));
        if (elementCount < 0) throw new ArgumentOutOfRangeException(nameof(elementCount));
        Code = code.Trim();
        Quantity = new QuantityValue(dimension, value);
        FactCount = factCount;
        ElementCount = elementCount;
    }

    public string Code { get; }
    public QuantityValue Quantity { get; }
    public int FactCount { get; }
    public int ElementCount { get; }
}

public static class QuantityAccumulator
{
    public static IReadOnlyList<QuantitySummary> Summarize(IEnumerable<QuantityFact> facts)
    {
        if (facts is null) throw new ArgumentNullException(nameof(facts));

        return facts
            .GroupBy(static fact => new QuantityKey(fact.Code, fact.Quantity.Dimension), QuantityKeyComparer.Instance)
            .Select(static group => CreateSummary(group.Key, group))
            .OrderBy(static summary => summary.Code, StringComparer.Ordinal)
            .ThenBy(static summary => summary.Quantity.Dimension)
            .ToArray();
    }

    private static QuantitySummary CreateSummary(QuantityKey key, IEnumerable<QuantityFact> facts)
    {
        var sum = 0d;
        var compensation = 0d;
        var factCount = 0;
        var elementIds = new HashSet<ElementId>();

        foreach (var fact in facts.OrderBy(static fact => fact.Quantity.Value))
        {
            var corrected = fact.Quantity.Value - compensation;
            var next = sum + corrected;
            compensation = (next - sum) - corrected;
            sum = next;
            factCount++;
            elementIds.Add(fact.ElementId);
            if (!Numeric.IsFinite(sum))
                throw new OverflowException($"Quantity total for '{key.Code}' is not representable as a finite double.");
        }

        return new QuantitySummary(key.Code, key.Dimension, sum, factCount, elementIds.Count);
    }

    private readonly struct QuantityKey
    {
        public QuantityKey(string code, QuantityDimension dimension)
        {
            Code = code;
            Dimension = dimension;
        }
        public string Code { get; }
        public QuantityDimension Dimension { get; }
    }

    private sealed class QuantityKeyComparer : IEqualityComparer<QuantityKey>
    {
        public static readonly QuantityKeyComparer Instance = new QuantityKeyComparer();
        public bool Equals(QuantityKey x, QuantityKey y) => x.Dimension == y.Dimension && StringComparer.Ordinal.Equals(x.Code, y.Code);
        public int GetHashCode(QuantityKey obj)
        {
            unchecked { return (StringComparer.Ordinal.GetHashCode(obj.Code) * 397) ^ (int)obj.Dimension; }
        }
    }
}

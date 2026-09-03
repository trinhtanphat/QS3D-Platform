using System.Collections;
using System.Globalization;
using System.Numerics;
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
        value = Numeric.RequireNonNegativeFinite(value, nameof(value));
        Value = value == 0d ? 0d : value;
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

        var quantity = new QuantityValue(dimension, value);
        if (factCount == 0)
        {
            if (elementCount != 0)
                throw new ArgumentException("Element count must be zero when fact count is zero.", nameof(elementCount));
            if (quantity.Value != 0d)
                throw new ArgumentException("Quantity value must be zero when fact count is zero.", nameof(value));
        }
        else
        {
            if (elementCount == 0)
                throw new ArgumentException("Element count must be positive when fact count is positive.", nameof(elementCount));
            if (elementCount > factCount)
                throw new ArgumentException("Element count must not exceed fact count.", nameof(elementCount));
        }

        Code = code.Trim();
        Quantity = quantity;
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
    private const int MaximumFacts = 100_000;
    private const long FractionMask = 0x000fffffffffffffL;
    private const ulong HiddenBit = 1UL << 52;

    public static IReadOnlyList<QuantitySummary> Summarize(IEnumerable<QuantityFact> facts)
    {
        if (facts is null) throw new ArgumentNullException(nameof(facts));

        var copiedFacts = MaterializeFacts(facts);
        if (copiedFacts.Any(static fact => fact is null))
            throw new ArgumentException("Quantity facts must not contain null entries.", nameof(facts));
        ValidateSourceProvenance(copiedFacts);
        ValidateCodeDimensionAffinity(copiedFacts);

        var summaries = copiedFacts
            .GroupBy(static fact => new QuantityKey(fact.Code, fact.Quantity.Dimension), QuantityKeyComparer.Instance)
            .Select(static group => CreateSummary(group.Key, group))
            .OrderBy(static summary => summary.Code, StringComparer.Ordinal)
            .ThenBy(static summary => summary.Quantity.Dimension)
            .ToArray();
        return Array.AsReadOnly(summaries);
    }

    private static QuantityFact[] MaterializeFacts(IEnumerable<QuantityFact> facts)
    {
        int? advertisedCount = null;
        CaptureCount(facts as ICollection<QuantityFact>, static collection => collection.Count, ref advertisedCount);
        CaptureCount(facts as IReadOnlyCollection<QuantityFact>, static collection => collection.Count, ref advertisedCount);
        CaptureCount(facts as ICollection, static collection => collection.Count, ref advertisedCount);

        var copied = advertisedCount.HasValue ? new List<QuantityFact>(advertisedCount.Value) : new List<QuantityFact>();
        foreach (var fact in facts)
        {
            if (copied.Count >= MaximumFacts)
                throw new InvalidOperationException($"Quantity facts exceed the supported maximum of {MaximumFacts} entries.");
            copied.Add(fact);
        }

        if (advertisedCount.HasValue && advertisedCount.Value != copied.Count)
            throw new InvalidOperationException("Quantity facts changed cardinality during materialization.");

        RequireStableKnownCount(facts, advertisedCount, copied.Count);
        RequireStableFactGeneration(facts, advertisedCount, copied);
        return copied.ToArray();
    }

    private static void RequireStableFactGeneration(
        IEnumerable<QuantityFact> facts,
        int? advertisedCount,
        IReadOnlyList<QuantityFact> snapshot)
    {
        if (!advertisedCount.HasValue)
            return;

        RequireStableKnownCount(facts, advertisedCount, snapshot.Count);
        var index = 0;
        using (var enumerator = facts.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                if (index >= snapshot.Count || !QuantityFactStateEquals(snapshot[index], enumerator.Current))
                    throw new InvalidOperationException("Quantity facts content changed during materialization.");
                index++;
            }
        }

        if (index != snapshot.Count)
            throw new InvalidOperationException("Quantity facts content changed during materialization.");
        RequireStableKnownCount(facts, advertisedCount, snapshot.Count);
    }

    private static bool QuantityFactStateEquals(QuantityFact? left, QuantityFact? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        return left.ElementId.Equals(right.ElementId)
            && StringComparer.Ordinal.Equals(left.Code, right.Code)
            && left.Quantity.Equals(right.Quantity)
            && Nullable.Equals(left.SourceReference, right.SourceReference);
    }

    private static void RequireStableKnownCount(
        IEnumerable<QuantityFact> facts,
        int? advertisedCount,
        int materializedCount)
    {
        int? currentCount = null;
        CaptureCount(facts as ICollection<QuantityFact>, static collection => collection.Count, ref currentCount);
        CaptureCount(facts as IReadOnlyCollection<QuantityFact>, static collection => collection.Count, ref currentCount);
        CaptureCount(facts as ICollection, static collection => collection.Count, ref currentCount);

        if (currentCount.HasValue && currentCount.Value != materializedCount)
            throw new InvalidOperationException("Quantity facts changed cardinality during materialization.");
        if (advertisedCount.HasValue != currentCount.HasValue
            || (advertisedCount.HasValue && advertisedCount.Value != currentCount!.Value))
            throw new InvalidOperationException("Quantity facts changed cardinality during materialization.");
    }

    private static void CaptureCount<TCollection>(TCollection? collection, Func<TCollection, int> getCount, ref int? advertisedCount)
        where TCollection : class
    {
        if (collection is null) return;
        var count = getCount(collection);
        if (count < 0)
            throw new ArgumentException("Quantity facts reported a negative Count.", "facts");
        if (count > MaximumFacts)
            throw new InvalidOperationException($"Quantity facts exceed the supported maximum of {MaximumFacts} entries.");
        if (advertisedCount.HasValue && advertisedCount.Value != count)
            throw new InvalidOperationException("Quantity facts expose conflicting Count values.");
        advertisedCount = count;
    }

    private static void ValidateSourceProvenance(IEnumerable<QuantityFact> facts)
    {
        var sourceByElement = new Dictionary<ElementId, CadReference?>();
        foreach (var fact in facts)
        {
            if (sourceByElement.TryGetValue(fact.ElementId, out var existingSource))
            {
                if (existingSource != fact.SourceReference)
                    throw new InvalidOperationException($"Quantity facts for element {fact.ElementId.Value:D} contain conflicting CAD provenance across quantity keys.");
            }
            else
            {
                sourceByElement.Add(fact.ElementId, fact.SourceReference);
            }
        }
    }

    private static void ValidateCodeDimensionAffinity(IEnumerable<QuantityFact> facts)
    {
        var dimensionsByCode = new Dictionary<string, QuantityDimension>(StringComparer.Ordinal);
        foreach (var fact in facts)
        {
            if (dimensionsByCode.TryGetValue(fact.Code, out var existingDimension))
            {
                if (existingDimension != fact.Quantity.Dimension)
                    throw new InvalidOperationException($"Quantity code '{fact.Code}' is present with both {existingDimension} and {fact.Quantity.Dimension} dimensions.");
            }
            else
            {
                dimensionsByCode.Add(fact.Code, fact.Quantity.Dimension);
            }
        }
    }

    private static QuantitySummary CreateSummary(QuantityKey key, IEnumerable<QuantityFact> facts)
    {
        var exactUnits = BigInteger.Zero;
        var factCount = 0;
        var elementIds = new HashSet<ElementId>();
        var sourceByElement = new Dictionary<ElementId, CadReference?>();

        foreach (var fact in facts)
        {
            if (sourceByElement.TryGetValue(fact.ElementId, out var existingSource))
            {
                if (existingSource != fact.SourceReference)
                    throw new InvalidOperationException($"Quantity facts for element {fact.ElementId.Value:D} and '{key.Code}'/{key.Dimension} contain conflicting CAD provenance.");
            }
            else
            {
                sourceByElement.Add(fact.ElementId, fact.SourceReference);
            }

            AddExactDoubleUnits(ref exactUnits, fact.Quantity.Value);
            factCount++;
            elementIds.Add(fact.ElementId);
        }

        var sum = RoundExactUnitsToFiniteDouble(exactUnits, key.Code);
        return new QuantitySummary(key.Code, key.Dimension, sum, factCount, elementIds.Count);
    }

    private static void AddExactDoubleUnits(ref BigInteger exactUnits, double value)
    {
        if (value == 0d) return;

        var bits = BitConverter.DoubleToInt64Bits(value);
        var rawExponent = (int)((bits >> 52) & 0x7ffL);
        var fraction = (ulong)(bits & FractionMask);
        if (rawExponent == 0)
        {
            exactUnits += new BigInteger(fraction);
            return;
        }

        var significand = HiddenBit | fraction;
        exactUnits += new BigInteger(significand) << (rawExponent - 1);
    }

    private static double RoundExactUnitsToFiniteDouble(BigInteger exactUnits, string code)
    {
        if (exactUnits.IsZero) return 0d;
        if (exactUnits.Sign < 0)
            throw new InvalidOperationException("Quantity accumulator exact sum became negative.");

        var bitLength = GetPositiveBitLength(exactUnits);
        if (bitLength <= 52)
        {
            var subnormalBits = (ulong)exactUnits;
            return BitConverter.Int64BitsToDouble((long)subnormalBits);
        }

        var shift = bitLength - 53;
        var roundedSignificand = exactUnits >> shift;
        if (shift > 0)
        {
            var remainder = exactUnits - (roundedSignificand << shift);
            var half = BigInteger.One << (shift - 1);
            if (remainder > half || (remainder == half && !roundedSignificand.IsEven))
                roundedSignificand += BigInteger.One;
        }

        if (roundedSignificand == (BigInteger.One << 53))
        {
            roundedSignificand >>= 1;
            shift++;
        }

        var rawExponent = shift + 1;
        if (rawExponent >= 0x7ff)
            throw new OverflowException($"Quantity total for '{code}' is not representable as a finite double.");

        var significand = (ulong)roundedSignificand;
        var fraction = significand - HiddenBit;
        var resultBits = ((ulong)rawExponent << 52) | fraction;
        return BitConverter.Int64BitsToDouble((long)resultBits);
    }

    private static int GetPositiveBitLength(BigInteger value)
    {
        var bytes = value.ToByteArray();
        var highestIndex = bytes.Length - 1;
        while (highestIndex > 0 && bytes[highestIndex] == 0)
            highestIndex--;

        var highest = bytes[highestIndex];
        var bitsInHighest = 0;
        while (highest != 0)
        {
            bitsInHighest++;
            highest >>= 1;
        }
        return highestIndex * 8 + bitsInHighest;
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

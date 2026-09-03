using System.Collections;
using System.Globalization;
using System.Numerics;
using QS3D.Platform.Domain;
using QS3D.Platform.Geometry;

namespace QS3D.Platform.Quantity;

public sealed class QuantityFactor
{
    public QuantityFactor(string propertyName, QuantityUnit unit, int exponent = 1)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Quantity factor property name must not be blank.", nameof(propertyName));
        if (!Enum.IsDefined(typeof(QuantityUnit), unit)) throw new ArgumentOutOfRangeException(nameof(unit));
        if (exponent < 1 || exponent > 3) throw new ArgumentOutOfRangeException(nameof(exponent), exponent, "Quantity factor exponent must be between 1 and 3.");
        PropertyName = propertyName.Trim();
        Unit = unit;
        Exponent = exponent;
    }

    public string PropertyName { get; }
    public QuantityUnit Unit { get; }
    public int Exponent { get; }
}

public sealed class QuantityRuleDefinition
{
    public QuantityRuleDefinition(
        SemanticElementKind elementKind,
        string code,
        QuantityDimension outputDimension,
        IEnumerable<QuantityFactor>? factors = null,
        double multiplier = 1d,
        string? description = null)
    {
        if (elementKind == SemanticElementKind.Unknown || !Enum.IsDefined(typeof(SemanticElementKind), elementKind)) throw new ArgumentOutOfRangeException(nameof(elementKind));
        if (!Enum.IsDefined(typeof(QuantityDimension), outputDimension)) throw new ArgumentOutOfRangeException(nameof(outputDimension));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Quantity rule code must not be blank.", nameof(code));
        multiplier = Numeric.RequireNonNegativeFinite(multiplier, nameof(multiplier));
        var copiedFactors = factors is null
            ? Array.Empty<QuantityFactor>()
            : QuantityRuleMaterializer.Materialize(factors, nameof(factors), "quantity rule factors");
        if (copiedFactors.Any(static factor => factor is null)) throw new ArgumentException("Quantity rule factors must not contain null entries.", nameof(factors));

        var inferred = InferDimension(copiedFactors);
        if (inferred != outputDimension)
            throw new InvalidOperationException($"Quantity rule '{code.Trim()}' factors produce {inferred}, not declared {outputDimension}.");

        ElementKind = elementKind;
        Code = code.Trim();
        OutputDimension = outputDimension;
        Factors = Array.AsReadOnly(copiedFactors);
        Multiplier = multiplier;
        Description = description is null || string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public SemanticElementKind ElementKind { get; }
    public string Code { get; }
    public QuantityDimension OutputDimension { get; }
    public IReadOnlyList<QuantityFactor> Factors { get; }
    public double Multiplier { get; }
    public string? Description { get; }

    private static QuantityDimension InferDimension(IReadOnlyList<QuantityFactor> factors)
    {
        var lengthPower = 0;
        var massPower = 0;
        foreach (var factor in factors)
        {
            var exponent = factor.Exponent;
            switch (QuantityUnits.DimensionOf(factor.Unit))
            {
                case QuantityDimension.Count:
                    break;
                case QuantityDimension.Length:
                    lengthPower += exponent;
                    break;
                case QuantityDimension.Area:
                    lengthPower += 2 * exponent;
                    break;
                case QuantityDimension.Volume:
                    lengthPower += 3 * exponent;
                    break;
                case QuantityDimension.Mass:
                    massPower += exponent;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(factors), "Unsupported quantity factor dimension.");
            }
        }

        if (massPower == 0)
        {
            switch (lengthPower)
            {
                case 0: return QuantityDimension.Count;
                case 1: return QuantityDimension.Length;
                case 2: return QuantityDimension.Area;
                case 3: return QuantityDimension.Volume;
            }
        }
        else if (massPower == 1 && lengthPower == 0)
        {
            return QuantityDimension.Mass;
        }

        throw new InvalidOperationException($"Quantity factors produce an unsupported composite dimension (length power {lengthPower}, mass power {massPower}).");
    }
}

public sealed class QuantityRuleCatalog
{
    private readonly QuantityRuleDefinition[] _rules;
    private readonly IReadOnlyList<QuantityRuleDefinition> _rulesView;

    public QuantityRuleCatalog(IEnumerable<QuantityRuleDefinition> rules)
    {
        if (rules is null) throw new ArgumentNullException(nameof(rules));
        var copied = QuantityRuleMaterializer.Materialize(rules, nameof(rules), "quantity rule catalog entries");
        if (copied.Any(static rule => rule is null)) throw new ArgumentException("Quantity rule catalog must not contain null entries.", nameof(rules));

        var unique = new HashSet<string>(StringComparer.Ordinal);
        var dimensionsByCode = new Dictionary<string, QuantityDimension>(StringComparer.Ordinal);
        foreach (var rule in copied)
        {
            var key = ((int)rule.ElementKind).ToString(CultureInfo.InvariantCulture) + "\u001f" + rule.Code;
            if (!unique.Add(key)) throw new InvalidOperationException($"Duplicate quantity rule '{rule.Code}' for {rule.ElementKind}.");
            if (dimensionsByCode.TryGetValue(rule.Code, out var existingDimension) && existingDimension != rule.OutputDimension)
                throw new InvalidOperationException($"Quantity code '{rule.Code}' is declared with both {existingDimension} and {rule.OutputDimension} dimensions.");
            dimensionsByCode[rule.Code] = rule.OutputDimension;
        }

        _rules = copied.OrderBy(static rule => rule.ElementKind)
            .ThenBy(static rule => rule.Code, StringComparer.Ordinal)
            .ToArray();
        _rulesView = Array.AsReadOnly(_rules);
    }

    public IReadOnlyList<QuantityRuleDefinition> Rules => _rulesView;

    public IReadOnlyList<QuantityRuleDefinition> ForKind(SemanticElementKind kind)
    {
        if (kind == SemanticElementKind.Unknown || !Enum.IsDefined(typeof(SemanticElementKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        return Array.AsReadOnly(_rules.Where(rule => rule.ElementKind == kind).ToArray());
    }
}

internal static class QuantityRuleMaterializer
{
    internal const int MaximumEntries = 100_000;

    internal static T[] Materialize<T>(IEnumerable<T> source, string parameterName, string entryDescription)
    {
        if (source is null) throw new ArgumentNullException(parameterName);
        if (string.IsNullOrWhiteSpace(entryDescription)) throw new ArgumentException("Entry description must not be blank.", nameof(entryDescription));

        int? advertisedCount = null;
        CaptureCount(source as ICollection<T>, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);
        CaptureCount(source as IReadOnlyCollection<T>, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);
        CaptureCount(source as ICollection, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);

        var copied = advertisedCount.HasValue ? new List<T>(advertisedCount.Value) : new List<T>();
        foreach (var item in source)
        {
            if (copied.Count >= MaximumEntries)
                throw new InvalidOperationException($"{entryDescription} exceed the supported maximum of {MaximumEntries} entries.");
            copied.Add(item);
        }

        if (advertisedCount.HasValue && advertisedCount.Value != copied.Count)
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");

        int? finalCount = null;
        CaptureCount(source as ICollection<T>, static collection => collection.Count, ref finalCount, parameterName, entryDescription);
        CaptureCount(source as IReadOnlyCollection<T>, static collection => collection.Count, ref finalCount, parameterName, entryDescription);
        CaptureCount(source as ICollection, static collection => collection.Count, ref finalCount, parameterName, entryDescription);
        if (advertisedCount.HasValue != finalCount.HasValue
            || (advertisedCount.HasValue && advertisedCount.Value != finalCount!.Value))
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");

        return copied.ToArray();
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

public static class QuantityRuleEngine
{
    private const int MaximumFacts = 100_000;
    private const long FractionMask = 0x000fffffffffffffL;
    private const ulong HiddenBit = 1UL << 52;

    public static IReadOnlyList<QuantityFact> Evaluate(
        SemanticProject project,
        QuantityRuleCatalog catalog,
        bool skipRuleWhenInputMissing = false)
    {
        if (project is null) throw new ArgumentNullException(nameof(project));
        if (catalog is null) throw new ArgumentNullException(nameof(catalog));

        var facts = new List<QuantityFact>();
        foreach (var element in project.Elements.OrderBy(static element => element.Id.Value))
        {
            foreach (var rule in catalog.ForKind(element.Kind))
            {
                var value = TryEvaluate(element, rule, skipRuleWhenInputMissing, out var missingInput);
                if (missingInput) continue;
                if (facts.Count >= MaximumFacts)
                    throw new InvalidOperationException($"Evaluated quantity facts exceed the supported maximum of {MaximumFacts} entries.");
                facts.Add(new QuantityFact(
                    element.Id,
                    rule.Code,
                    new QuantityValue(rule.OutputDimension, value),
                    element.SourceReference));
            }
        }
        return facts.AsReadOnly();
    }

    private static double TryEvaluate(
        SemanticElement element,
        QuantityRuleDefinition rule,
        bool skipRuleWhenInputMissing,
        out bool missingInput)
    {
        missingInput = false;
        var productFactors = new List<double>(1 + rule.Factors.Count * 6);
        productFactors.Add(rule.Multiplier);

        foreach (var factor in rule.Factors)
        {
            if (!element.Properties.TryGetValue(factor.PropertyName, out var raw))
            {
                if (skipRuleWhenInputMissing)
                {
                    missingInput = true;
                    return 0d;
                }
                throw new InvalidOperationException($"Element '{element.Name}' is missing property '{factor.PropertyName}' required by quantity rule '{rule.Code}'.");
            }

            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || !Numeric.IsFinite(parsed) || parsed < 0d)
                throw new InvalidOperationException($"Element '{element.Name}' property '{factor.PropertyName}' must be a non-negative finite invariant-culture number for quantity rule '{rule.Code}'.");

            var canonicalScale = QuantityUnits.ToCanonical(1d, factor.Unit);
            for (var i = 0; i < factor.Exponent; i++)
            {
                productFactors.Add(parsed);
                productFactors.Add(canonicalScale);
            }
        }

        return MultiplyCanonicalFactors(productFactors, rule.Code, element.Name);
    }

    private static double MultiplyCanonicalFactors(List<double> factors, string ruleCode, string elementName)
    {
        if (factors.Any(static factor => factor == 0d))
            return 0d;

        var significands = new List<ulong>(factors.Count);
        long binaryExponent = 0;
        foreach (var factor in factors)
        {
            if (factor == 1d) continue;
            DecomposeExactPositiveFinite(factor, out var significand, out var exponent);
            significands.Add(significand);
            binaryExponent += exponent;
        }

        if (significands.Count == 0)
            return 1d;

        var exactSignificand = MultiplySignificandsBalanced(significands, 0, significands.Count);
        return RoundExactBinaryProduct(exactSignificand, binaryExponent, ruleCode, elementName);
    }

    private static void DecomposeExactPositiveFinite(double value, out ulong significand, out int binaryExponent)
    {
        var bits = BitConverter.DoubleToInt64Bits(value);
        var rawExponent = (int)((bits >> 52) & 0x7ffL);
        var fraction = (ulong)(bits & FractionMask);
        if (rawExponent == 0)
        {
            significand = fraction;
            binaryExponent = -1074;
            return;
        }

        significand = HiddenBit | fraction;
        binaryExponent = rawExponent - 1023 - 52;
    }

    private static BigInteger MultiplySignificandsBalanced(IReadOnlyList<ulong> significands, int start, int count)
    {
        if (count == 1)
            return new BigInteger(significands[start]);
        if (count <= 8)
        {
            var product = BigInteger.One;
            for (var i = 0; i < count; i++)
                product *= significands[start + i];
            return product;
        }

        var leftCount = count / 2;
        var left = MultiplySignificandsBalanced(significands, start, leftCount);
        var right = MultiplySignificandsBalanced(significands, start + leftCount, count - leftCount);
        return left * right;
    }

    private static double RoundExactBinaryProduct(
        BigInteger exactSignificand,
        long binaryExponent,
        string ruleCode,
        string elementName)
    {
        var bitLength = GetPositiveBitLength(exactSignificand);
        var highestBinaryExponent = binaryExponent + bitLength - 1L;

        if (highestBinaryExponent < -1022L)
        {
            var unitShift = binaryExponent + 1074L;
            BigInteger roundedUnits;
            if (unitShift >= 0L)
            {
                roundedUnits = exactSignificand << checked((int)unitShift);
            }
            else
            {
                roundedUnits = RoundRightToNearestEven(exactSignificand, checked((int)-unitShift));
            }

            if (roundedUnits.IsZero)
                throw new OverflowException($"Quantity rule '{ruleCode}' result underflowed for element '{elementName}'.");
            if (roundedUnits > new BigInteger(HiddenBit))
                throw new InvalidOperationException("Quantity rule exact subnormal rounding exceeded the minimum-normal boundary.");
            return BitConverter.Int64BitsToDouble((long)(ulong)roundedUnits);
        }

        var significandShift = bitLength - 53;
        BigInteger roundedSignificand;
        if (significandShift > 0)
            roundedSignificand = RoundRightToNearestEven(exactSignificand, significandShift);
        else
            roundedSignificand = exactSignificand << -significandShift;

        var representedExponent = binaryExponent + significandShift;
        if (roundedSignificand == (BigInteger.One << 53))
        {
            roundedSignificand >>= 1;
            representedExponent++;
        }

        var rawExponent = representedExponent + 1075L;
        if (rawExponent >= 0x7ffL)
            throw new OverflowException($"Quantity rule '{ruleCode}' result overflowed for element '{elementName}'.");
        if (rawExponent <= 0L)
            throw new InvalidOperationException("Quantity rule exact normal rounding crossed below the normal exponent range.");

        var significand = (ulong)roundedSignificand;
        var fraction = significand - HiddenBit;
        var bits = ((ulong)rawExponent << 52) | fraction;
        var result = BitConverter.Int64BitsToDouble((long)bits);
        if (!Numeric.IsFinite(result))
            throw new OverflowException($"Quantity rule '{ruleCode}' result overflowed for element '{elementName}'.");
        return result;
    }

    private static BigInteger RoundRightToNearestEven(BigInteger value, int shift)
    {
        if (shift <= 0) return value << -shift;

        var bitLength = GetPositiveBitLength(value);
        if (shift > bitLength)
            return BigInteger.Zero;
        if (shift == bitLength)
        {
            var half = BigInteger.One << (bitLength - 1);
            return value == half ? BigInteger.Zero : BigInteger.One;
        }

        var quotient = value >> shift;
        var remainder = value - (quotient << shift);
        var midpoint = BigInteger.One << (shift - 1);
        if (remainder > midpoint || (remainder == midpoint && !quotient.IsEven))
            quotient += BigInteger.One;
        return quotient;
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
}

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
            : QuantityRuleMaterializer.MaterializeStableFactors(factors, nameof(factors), "quantity rule factors");
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
    private static readonly IReadOnlyList<QuantityRuleDefinition> EmptyRules = Array.AsReadOnly(Array.Empty<QuantityRuleDefinition>());
    private readonly QuantityRuleDefinition[] _rules;
    private readonly IReadOnlyList<QuantityRuleDefinition> _rulesView;
    private readonly IReadOnlyDictionary<SemanticElementKind, IReadOnlyList<QuantityRuleDefinition>> _rulesByKind;

    public QuantityRuleCatalog(IEnumerable<QuantityRuleDefinition> rules)
    {
        if (rules is null) throw new ArgumentNullException(nameof(rules));
        var copied = QuantityRuleMaterializer.MaterializeStableRules(rules, nameof(rules), "quantity rule catalog entries");
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
        _rulesByKind = _rules
            .GroupBy(static rule => rule.ElementKind)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<QuantityRuleDefinition>)Array.AsReadOnly(group.ToArray()));
    }

    public IReadOnlyList<QuantityRuleDefinition> Rules => _rulesView;

    public IReadOnlyList<QuantityRuleDefinition> ForKind(SemanticElementKind kind)
    {
        if (kind == SemanticElementKind.Unknown || !Enum.IsDefined(typeof(SemanticElementKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        return _rulesByKind.TryGetValue(kind, out var rules) ? rules : EmptyRules;
    }
}

internal static class QuantityRuleMaterializer
{
    internal const int MaximumEntries = 100_000;

    internal static QuantityFactor[] MaterializeStableFactors(
        IEnumerable<QuantityFactor> source,
        string parameterName,
        string entryDescription) =>
        MaterializeStable(source, parameterName, entryDescription, QuantityFactorStateEquals);

    internal static QuantityRuleDefinition[] MaterializeStableRules(
        IEnumerable<QuantityRuleDefinition> source,
        string parameterName,
        string entryDescription) =>
        MaterializeStable(source, parameterName, entryDescription, QuantityRuleStateEquals);

    private static T[] MaterializeStable<T>(
        IEnumerable<T> source,
        string parameterName,
        string entryDescription,
        Func<T?, T?, bool> stateEquals)
    {
        if (source is null) throw new ArgumentNullException(parameterName);
        if (string.IsNullOrWhiteSpace(entryDescription)) throw new ArgumentException("Entry description must not be blank.", nameof(entryDescription));
        if (stateEquals is null) throw new ArgumentNullException(nameof(stateEquals));

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

        RequireStableCount(source, advertisedCount, parameterName, entryDescription);
        if (!advertisedCount.HasValue)
            return copied.ToArray();

        var snapshot = copied.ToArray();
        var index = 0;
        using (var enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                if (index >= snapshot.Length || !stateEquals(snapshot[index], enumerator.Current))
                    throw new InvalidOperationException($"{entryDescription} content changed during materialization.");
                index++;
            }
        }

        if (index != snapshot.Length)
            throw new InvalidOperationException($"{entryDescription} content changed during materialization.");
        RequireStableCount(source, advertisedCount, parameterName, entryDescription);
        return snapshot;
    }

    private static void RequireStableCount<T>(
        IEnumerable<T> source,
        int? advertisedCount,
        string parameterName,
        string entryDescription)
    {
        int? observedCount = null;
        CaptureCount(source as ICollection<T>, static collection => collection.Count, ref observedCount, parameterName, entryDescription);
        CaptureCount(source as IReadOnlyCollection<T>, static collection => collection.Count, ref observedCount, parameterName, entryDescription);
        CaptureCount(source as ICollection, static collection => collection.Count, ref observedCount, parameterName, entryDescription);
        if (advertisedCount.HasValue != observedCount.HasValue
            || (advertisedCount.HasValue && advertisedCount.Value != observedCount!.Value))
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");
    }

    private static bool QuantityFactorStateEquals(QuantityFactor? left, QuantityFactor? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return string.Equals(left.PropertyName, right.PropertyName, StringComparison.Ordinal)
            && left.Unit == right.Unit
            && left.Exponent == right.Exponent;
    }

    private static bool QuantityRuleStateEquals(QuantityRuleDefinition? left, QuantityRuleDefinition? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        if (left.ElementKind != right.ElementKind
            || !string.Equals(left.Code, right.Code, StringComparison.Ordinal)
            || left.OutputDimension != right.OutputDimension
            || !left.Multiplier.Equals(right.Multiplier)
            || !string.Equals(left.Description, right.Description, StringComparison.Ordinal)
            || left.Factors.Count != right.Factors.Count)
            return false;

        for (var index = 0; index < left.Factors.Count; index++)
        {
            if (!QuantityFactorStateEquals(left.Factors[index], right.Factors[index]))
                return false;
        }
        return true;
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

        var elements = project.Elements
            .OrderBy(static element => element.Id.Value)
            .Select(CaptureElementSnapshot)
            .ToArray();

        var facts = new List<QuantityFact>();
        foreach (var element in elements)
        {
            foreach (var rule in catalog.ForKind(element.Kind))
            {
                if (!skipRuleWhenInputMissing && facts.Count >= MaximumFacts)
                    throw new InvalidOperationException($"Evaluated quantity facts exceed the supported maximum of {MaximumFacts} entries.");

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

    private static ElementSnapshot CaptureElementSnapshot(SemanticElement element)
    {
        var sourceBefore = element.SourceReference;
        var firstProperties = CaptureProperties(element.Properties);
        var sourceBetween = element.SourceReference;
        var secondProperties = CaptureProperties(element.Properties);
        var sourceAfter = element.SourceReference;

        if (sourceBefore != sourceBetween
            || sourceBetween != sourceAfter
            || !PropertySnapshotsEqual(firstProperties, secondProperties))
            throw new InvalidOperationException($"Element '{element.Name}' changed during quantity rule snapshot capture.");

        var properties = new Dictionary<string, string>(firstProperties.Length, StringComparer.Ordinal);
        foreach (var pair in firstProperties)
            properties.Add(pair.Key, pair.Value);

        return new ElementSnapshot(
            element.Id,
            element.Kind,
            element.Name,
            sourceBefore,
            properties);
    }

    private static KeyValuePair<string, string>[] CaptureProperties(IReadOnlyDictionary<string, string> properties) =>
        properties
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .ToArray();

    private static bool PropertySnapshotsEqual(
        IReadOnlyList<KeyValuePair<string, string>> left,
        IReadOnlyList<KeyValuePair<string, string>> right)
    {
        if (left.Count != right.Count) return false;
        for (var index = 0; index < left.Count; index++)
        {
            if (!string.Equals(left[index].Key, right[index].Key, StringComparison.Ordinal)
                || !string.Equals(left[index].Value, right[index].Value, StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static double TryEvaluate(
        ElementSnapshot element,
        QuantityRuleDefinition rule,
        bool skipRuleWhenInputMissing,
        out bool missingInput)
    {
        missingInput = false;
        if (skipRuleWhenInputMissing)
        {
            foreach (var factor in rule.Factors)
            {
                if (!element.Properties.ContainsKey(factor.PropertyName))
                {
                    missingInput = true;
                    return 0d;
                }
            }
        }

        var productFactors = new List<double>(1 + rule.Factors.Count * 3);
        productFactors.Add(rule.Multiplier);
        long decimalScalePower = 0;

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

            var unitScalePower = DecimalScalePowerToCanonical(factor.Unit);
            for (var i = 0; i < factor.Exponent; i++)
            {
                productFactors.Add(parsed);
                decimalScalePower += unitScalePower;
            }
        }

        return MultiplyCanonicalFactors(productFactors, decimalScalePower, rule.Code, element.Name);
    }

    private static int DecimalScalePowerToCanonical(QuantityUnit unit)
    {
        switch (unit)
        {
            case QuantityUnit.Each: return 0;
            case QuantityUnit.Millimeter: return -3;
            case QuantityUnit.Centimeter: return -2;
            case QuantityUnit.Meter: return 0;
            case QuantityUnit.SquareMillimeter: return -6;
            case QuantityUnit.SquareCentimeter: return -4;
            case QuantityUnit.SquareMeter: return 0;
            case QuantityUnit.CubicMillimeter: return -9;
            case QuantityUnit.CubicCentimeter: return -6;
            case QuantityUnit.CubicMeter: return 0;
            case QuantityUnit.Gram: return -3;
            case QuantityUnit.Kilogram: return 0;
            case QuantityUnit.Tonne: return 3;
            default: throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported quantity unit.");
        }
    }

    private static double MultiplyCanonicalFactors(
        List<double> factors,
        long decimalScalePower,
        string ruleCode,
        string elementName)
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

        var exactNumerator = significands.Count == 0
            ? BigInteger.One
            : MultiplySignificandsBalanced(significands, 0, significands.Count);
        var exactDenominator = BigInteger.One;

        if (decimalScalePower > 0L)
        {
            var power = CheckedFivePower(decimalScalePower);
            exactNumerator *= BigInteger.Pow(5, power);
            binaryExponent += decimalScalePower;
        }
        else if (decimalScalePower < 0L)
        {
            var power = CheckedFivePower(-decimalScalePower);
            exactDenominator = BigInteger.Pow(5, power);
            binaryExponent += decimalScalePower;
        }

        return RoundExactRationalProduct(exactNumerator, exactDenominator, binaryExponent, ruleCode, elementName);
    }

    private static int CheckedFivePower(long power)
    {
        if (power < 0L || power > int.MaxValue)
            throw new InvalidOperationException("Quantity rule decimal scale exceeded the supported exact-rational exponent range.");
        return (int)power;
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

    private static double RoundExactRationalProduct(
        BigInteger numerator,
        BigInteger denominator,
        long binaryExponent,
        string ruleCode,
        string elementName)
    {
        if (numerator.Sign <= 0 || denominator.Sign <= 0)
            throw new InvalidOperationException("Quantity rule exact rational product must be positive before rounding.");

        var rationalExponent = FloorLog2Ratio(numerator, denominator);
        var highestBinaryExponent = binaryExponent + rationalExponent;

        if (highestBinaryExponent > 1023L)
            throw new OverflowException($"Quantity rule '{ruleCode}' result overflowed for element '{elementName}'.");
        if (highestBinaryExponent < -1075L)
            throw new OverflowException($"Quantity rule '{ruleCode}' result underflowed for element '{elementName}'.");

        if (highestBinaryExponent < -1022L)
        {
            var roundedUnits = RoundRatioByPowerOfTwo(numerator, denominator, binaryExponent + 1074L);
            if (roundedUnits.IsZero)
                throw new OverflowException($"Quantity rule '{ruleCode}' result underflowed for element '{elementName}'.");
            if (roundedUnits > new BigInteger(HiddenBit))
                throw new InvalidOperationException("Quantity rule exact subnormal rounding exceeded the minimum-normal boundary.");
            return BitConverter.Int64BitsToDouble((long)(ulong)roundedUnits);
        }

        var representedExponent = highestBinaryExponent - 52L;
        var roundedSignificand = RoundRatioByPowerOfTwo(
            numerator,
            denominator,
            binaryExponent - representedExponent);

        var twiceHiddenBit = BigInteger.One << 53;
        if (roundedSignificand == twiceHiddenBit)
        {
            roundedSignificand >>= 1;
            representedExponent++;
        }
        else if (roundedSignificand < new BigInteger(HiddenBit) || roundedSignificand > twiceHiddenBit)
        {
            throw new InvalidOperationException("Quantity rule exact rational rounding produced an invalid normal significand.");
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

    private static int FloorLog2Ratio(BigInteger numerator, BigInteger denominator)
    {
        var numeratorBits = GetPositiveBitLength(numerator);
        var denominatorBits = GetPositiveBitLength(denominator);
        var candidate = numeratorBits - denominatorBits;

        int comparison;
        if (candidate >= 0)
            comparison = numerator.CompareTo(denominator << candidate);
        else
            comparison = (numerator << -candidate).CompareTo(denominator);

        return comparison >= 0 ? candidate : candidate - 1;
    }

    private static BigInteger RoundRatioByPowerOfTwo(
        BigInteger numerator,
        BigInteger denominator,
        long binaryShift)
    {
        if (binaryShift >= 0L)
            numerator <<= CheckedShift(binaryShift);
        else
            denominator <<= CheckedShift(-binaryShift);

        var quotient = BigInteger.DivRem(numerator, denominator, out var remainder);
        var twiceRemainder = remainder << 1;
        var comparison = twiceRemainder.CompareTo(denominator);
        if (comparison > 0 || (comparison == 0 && !quotient.IsEven))
            quotient += BigInteger.One;
        return quotient;
    }

    private static int CheckedShift(long shift)
    {
        if (shift < 0L || shift > int.MaxValue)
            throw new InvalidOperationException("Quantity rule exact-rational binary shift exceeded the supported range.");
        return (int)shift;
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

    private sealed class ElementSnapshot
    {
        internal ElementSnapshot(
            ElementId id,
            SemanticElementKind kind,
            string name,
            CadReference? sourceReference,
            Dictionary<string, string> properties)
        {
            Id = id;
            Kind = kind;
            Name = name;
            SourceReference = sourceReference;
            Properties = properties;
        }

        internal ElementId Id { get; }
        internal SemanticElementKind Kind { get; }
        internal string Name { get; }
        internal CadReference? SourceReference { get; }
        internal IReadOnlyDictionary<string, string> Properties { get; }
    }
}

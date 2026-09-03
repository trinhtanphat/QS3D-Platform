using System.Collections;
using System.Globalization;
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
    private const double TwoTo52 = 4503599627370496d;
    private const double MinimumNormal = 2.2250738585072014e-308d;

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

        factors.Sort();
        var mantissa = 1d;
        long exponent = 0;

        foreach (var factor in factors)
        {
            DecomposePositiveFinite(factor, out var factorMantissa, out var factorExponent);
            mantissa *= factorMantissa;
            exponent += factorExponent;
            if (mantissa >= 2d)
            {
                mantissa *= 0.5d;
                exponent++;
            }
        }

        var result = ComposeFiniteProduct(mantissa, exponent);
        if (!Numeric.IsFinite(result))
            throw new OverflowException($"Quantity rule '{ruleCode}' result overflowed for element '{elementName}'.");
        if (result == 0d)
            throw new OverflowException($"Quantity rule '{ruleCode}' result underflowed for element '{elementName}'.");
        return result;
    }

    private static void DecomposePositiveFinite(double value, out double mantissa, out int exponent)
    {
        var bits = BitConverter.DoubleToInt64Bits(value);
        var rawExponent = (int)((bits >> 52) & 0x7ffL);
        var fraction = bits & 0x000fffffffffffffL;

        if (rawExponent != 0)
        {
            exponent = rawExponent - 1023;
            mantissa = 1d + fraction / TwoTo52;
            return;
        }

        var significandBits = (ulong)fraction;
        var highestBit = 0;
        for (var cursor = significandBits; (cursor >>= 1) != 0; highestBit++)
        {
        }

        exponent = highestBit - 1074;
        mantissa = (double)significandBits / (1L << highestBit);
    }

    private static double ComposeFiniteProduct(double mantissa, long exponent)
    {
        if (exponent > 1023)
            return double.PositiveInfinity;
        if (exponent < -1075)
            return 0d;
        if (exponent >= -1022)
            return mantissa * Math.Pow(2d, exponent);

        return mantissa * Math.Pow(2d, exponent + 1022) * MinimumNormal;
    }
}

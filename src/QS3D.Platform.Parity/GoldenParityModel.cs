using QS3D.Platform.Diagnostics;
using QS3D.Platform.Persistence;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.Parity;

public sealed class GoldenDiagnosticExpectation
{
    public GoldenDiagnosticExpectation(string code, DiagnosticSeverity severity, Guid? elementId = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Diagnostic code must not be blank.", nameof(code));
        if (!Enum.IsDefined(typeof(DiagnosticSeverity), severity)) throw new ArgumentOutOfRangeException(nameof(severity), severity, "Diagnostic severity must be a defined value.");
        if (elementId == Guid.Empty) throw new ArgumentException("Element ID must not be empty when supplied.", nameof(elementId));
        Code = code.Trim();
        Severity = severity;
        ElementId = elementId;
    }
    public string Code { get; }
    public DiagnosticSeverity Severity { get; }
    public Guid? ElementId { get; }
}

public sealed class GoldenQuantityExpectation
{
    public GoldenQuantityExpectation(Guid elementId, string code, QuantityDimension dimension, double canonicalValue, double absoluteTolerance = 1e-9)
    {
        if (elementId == Guid.Empty) throw new ArgumentException("Element ID must not be empty.", nameof(elementId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Quantity code must not be blank.", nameof(code));
        if (!Enum.IsDefined(typeof(QuantityDimension), dimension)) throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Quantity dimension must be a defined value.");
        if (double.IsNaN(canonicalValue) || double.IsInfinity(canonicalValue) || canonicalValue < 0d) throw new ArgumentOutOfRangeException(nameof(canonicalValue));
        if (double.IsNaN(absoluteTolerance) || double.IsInfinity(absoluteTolerance) || absoluteTolerance < 0d) throw new ArgumentOutOfRangeException(nameof(absoluteTolerance));
        ElementId = elementId;
        Code = code.Trim();
        Dimension = dimension;
        CanonicalValue = canonicalValue;
        AbsoluteTolerance = absoluteTolerance;
    }
    public Guid ElementId { get; }
    public string Code { get; }
    public QuantityDimension Dimension { get; }
    public double CanonicalValue { get; }
    public double AbsoluteTolerance { get; }
}

public sealed class GoldenParityFixture
{
    public GoldenParityFixture(
        string id,
        SemanticProjectSnapshot snapshot,
        IEnumerable<QuantityRuleDefinition> quantityRules,
        IEnumerable<GoldenDiagnosticExpectation>? expectedDiagnostics = null,
        IEnumerable<GoldenQuantityExpectation>? expectedQuantities = null,
        bool skipRuleWhenInputMissing = false,
        bool rejectUnexpectedDiagnostics = true,
        bool rejectUnexpectedQuantities = true)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Fixture ID must not be blank.", nameof(id));
        Id = NormalizeId(id);
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        QuantityRules = Copy(quantityRules, nameof(quantityRules));
        ExpectedDiagnostics = expectedDiagnostics is null ? Array.Empty<GoldenDiagnosticExpectation>() : Copy(expectedDiagnostics, nameof(expectedDiagnostics));
        ExpectedQuantities = expectedQuantities is null ? Array.Empty<GoldenQuantityExpectation>() : Copy(expectedQuantities, nameof(expectedQuantities));
        SkipRuleWhenInputMissing = skipRuleWhenInputMissing;
        RejectUnexpectedDiagnostics = rejectUnexpectedDiagnostics;
        RejectUnexpectedQuantities = rejectUnexpectedQuantities;
        EnsureUniqueExpectations();
    }

    public string Id { get; }
    public SemanticProjectSnapshot Snapshot { get; }
    public IReadOnlyList<QuantityRuleDefinition> QuantityRules { get; }
    public IReadOnlyList<GoldenDiagnosticExpectation> ExpectedDiagnostics { get; }
    public IReadOnlyList<GoldenQuantityExpectation> ExpectedQuantities { get; }
    public bool SkipRuleWhenInputMissing { get; }
    public bool RejectUnexpectedDiagnostics { get; }
    public bool RejectUnexpectedQuantities { get; }

    internal static string DiagnosticKey(string code, DiagnosticSeverity severity, Guid? elementId)
        => ((int)severity).ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + code + "|" + (elementId.HasValue ? elementId.Value.ToString("D") : "-");

    internal static string QuantityKey(Guid elementId, string code, QuantityDimension dimension)
        => elementId.ToString("D") + "|" + code + "|" + ((int)dimension).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private void EnsureUniqueExpectations()
    {
        var diagnosticKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in ExpectedDiagnostics)
        {
            var key = DiagnosticKey(item.Code, item.Severity, item.ElementId);
            if (!diagnosticKeys.Add(key)) throw new InvalidOperationException($"Duplicate diagnostic expectation '{key}'.");
        }
        var quantityKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in ExpectedQuantities)
        {
            var key = QuantityKey(item.ElementId, item.Code, item.Dimension);
            if (!quantityKeys.Add(key)) throw new InvalidOperationException($"Duplicate quantity expectation '{key}'.");
        }
    }

    private static string NormalizeId(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        foreach (var character in normalized)
        {
            var valid = (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '.' || character == '-' || character == '_';
            if (!valid) throw new ArgumentException("Fixture ID contains an unsupported character.", nameof(value));
        }
        return normalized;
    }

    private static T[] Copy<T>(IEnumerable<T> values, string parameterName) where T : class
    {
        if (values is null) throw new ArgumentNullException(parameterName);
        var copied = values.ToArray();
        if (copied.Any(static item => item is null)) throw new ArgumentException("Fixture collection must not contain null entries.", parameterName);
        return copied;
    }
}

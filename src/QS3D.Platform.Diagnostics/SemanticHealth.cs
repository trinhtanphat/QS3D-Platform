using QS3D.Platform.Domain;

namespace QS3D.Platform.Diagnostics;

public enum DiagnosticSeverity
{
    Info = 0,
    Warning,
    Error
}

public sealed class DiagnosticFinding
{
    public DiagnosticFinding(string code, DiagnosticSeverity severity, string message, ElementId? elementId = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Diagnostic code must not be blank.", nameof(code));
        if (!Enum.IsDefined(typeof(DiagnosticSeverity), severity)) throw new ArgumentOutOfRangeException(nameof(severity));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Diagnostic message must not be blank.", nameof(message));
        if (elementId.HasValue && elementId.Value.Value == Guid.Empty) throw new ArgumentException("Diagnostic element ID must not be empty when supplied.", nameof(elementId));
        Code = code.Trim();
        Severity = severity;
        Message = message.Trim();
        ElementId = elementId;
    }

    public string Code { get; }
    public DiagnosticSeverity Severity { get; }
    public string Message { get; }
    public ElementId? ElementId { get; }
}

public sealed class ModelHealthReport
{
    public ModelHealthReport(IEnumerable<DiagnosticFinding> findings)
    {
        if (findings is null) throw new ArgumentNullException(nameof(findings));
        var copied = findings.ToArray();
        if (copied.Any(static finding => finding is null)) throw new ArgumentException("Health report findings must not contain null entries.", nameof(findings));
        Findings = copied.OrderByDescending(static x => x.Severity)
            .ThenBy(static x => x.Code, StringComparer.Ordinal)
            .ThenBy(static x => x.ElementId.HasValue ? x.ElementId.Value.Value : Guid.Empty)
            .ToArray();
    }

    public IReadOnlyList<DiagnosticFinding> Findings { get; }
    public bool IsReady => Findings.All(static finding => finding.Severity != DiagnosticSeverity.Error);
    public int ErrorCount => Findings.Count(static finding => finding.Severity == DiagnosticSeverity.Error);
    public int WarningCount => Findings.Count(static finding => finding.Severity == DiagnosticSeverity.Warning);
}

public static class SemanticHealthAnalyzer
{
    public static ModelHealthReport Analyze(SemanticProject project)
    {
        if (project is null) throw new ArgumentNullException(nameof(project));
        var findings = new List<DiagnosticFinding>();

        foreach (var element in project.Elements)
        {
            if (!project.TryGetFamily(element.FamilyId, out var family) || family is null)
            {
                findings.Add(new DiagnosticFinding("SEM_FAMILY_MISSING", DiagnosticSeverity.Error,
                    $"Element '{element.Name}' references a family that is not in the project.", element.Id));
            }
            else if (family.Kind != element.Kind)
            {
                findings.Add(new DiagnosticFinding("SEM_FAMILY_KIND_MISMATCH", DiagnosticSeverity.Error,
                    $"Element '{element.Name}' kind {element.Kind} does not match family kind {family.Kind}.", element.Id));
            }

            if (element.FloorId.HasValue && !project.ContainsFloor(element.FloorId.Value))
            {
                findings.Add(new DiagnosticFinding("SEM_FLOOR_MISSING", DiagnosticSeverity.Error,
                    $"Element '{element.Name}' references a floor that is not in the project.", element.Id));
            }

            if (element.ZoneId.HasValue && !project.ContainsZone(element.ZoneId.Value))
            {
                findings.Add(new DiagnosticFinding("SEM_ZONE_MISSING", DiagnosticSeverity.Error,
                    $"Element '{element.Name}' references a zone that is not in the project.", element.Id));
            }

            if (!element.SourceReference.HasValue && element.GeneratedReferences.Count == 0)
            {
                findings.Add(new DiagnosticFinding("SEM_CAD_REFERENCE_EMPTY", DiagnosticSeverity.Warning,
                    $"Element '{element.Name}' has no source or generated CAD reference.", element.Id));
            }
        }

        return new ModelHealthReport(findings);
    }
}

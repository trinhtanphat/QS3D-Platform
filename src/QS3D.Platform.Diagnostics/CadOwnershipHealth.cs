using QS3D.Platform.Domain;

namespace QS3D.Platform.Diagnostics;

public static class CadOwnershipHealthAnalyzer
{
    public static IReadOnlyList<DiagnosticFinding> Analyze(SemanticProject project)
    {
        if (project is null) throw new ArgumentNullException(nameof(project));
        var findings = new List<DiagnosticFinding>();
        var owners = new Dictionary<CadReference, Ownership>();

        foreach (var element in project.Elements.OrderBy(static element => element.Id.Value))
        {
            if (element.SourceReference.HasValue)
                Register(element, element.SourceReference.Value, "source", owners, findings);
            foreach (var generated in element.GeneratedReferences.OrderBy(static reference => reference.DrawingId.Value).ThenBy(static reference => reference.Handle))
                Register(element, generated, "generated", owners, findings);
        }

        return findings;
    }

    private static void Register(SemanticElement element, CadReference reference, string role, Dictionary<CadReference, Ownership> owners, List<DiagnosticFinding> findings)
    {
        if (!owners.TryGetValue(reference, out var existing))
        {
            owners.Add(reference, new Ownership(element.Id, element.Name, role));
            return;
        }

        findings.Add(new DiagnosticFinding(
            "SEM_CAD_REFERENCE_OWNERSHIP_CONFLICT",
            DiagnosticSeverity.Error,
            $"CAD reference {reference.DrawingId.Value:D}/{reference.Handle.Value} is owned as {existing.Role} by '{existing.ElementName}' ({existing.ElementId.Value:D}) and as {role} by '{element.Name}' ({element.Id.Value:D}).",
            element.Id));
    }

    private sealed class Ownership
    {
        public Ownership(ElementId elementId, string elementName, string role)
        {
            ElementId = elementId;
            ElementName = elementName;
            Role = role;
        }
        public ElementId ElementId { get; }
        public string ElementName { get; }
        public string Role { get; }
    }
}

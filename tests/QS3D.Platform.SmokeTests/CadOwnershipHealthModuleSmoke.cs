using System.Runtime.CompilerServices;
using QS3D.Platform.Diagnostics;
using QS3D.Platform.Domain;

namespace QS3D.Platform.SmokeTests;

internal static class CadOwnershipHealthModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var invalidSeverity = (DiagnosticSeverity)int.MaxValue;
        Throws<ArgumentOutOfRangeException>(() => new DiagnosticFinding("BAD_SEVERITY", invalidSeverity, "invalid"));
        Throws<ArgumentException>(() => new DiagnosticFinding("BAD_ELEMENT", DiagnosticSeverity.Error, "invalid", new ElementId(Guid.Empty)));
        Throws<ArgumentException>(() => new ModelHealthReport(new DiagnosticFinding[] { null! }));

        var drawing = DrawingId.New();
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall");
        var project = new SemanticProject(ProjectId.New(), "Ownership");
        project.AddFamily(family);

        var first = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W1", family.Id);
        first.SetSource(new CadReference(drawing, new CadHandle("A")));
        project.AddElement(first);
        var second = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W2", family.Id);
        second.SetSource(new CadReference(drawing, new CadHandle("000a")));
        project.AddElement(second);

        var report = ModelReadinessAnalyzer.Analyze(project);
        Require(!report.IsReady, "canonical duplicate source ownership must fail readiness");
        Equal(1, report.Findings.Count(static finding => finding.Code == "SEM_CAD_REFERENCE_OWNERSHIP_CONFLICT"));

        var sameElement = new SemanticProject(ProjectId.New(), "Source Generated Collision");
        sameElement.AddFamily(family);
        var third = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W3", family.Id);
        third.SetSource(new CadReference(drawing, new CadHandle("B")));
        third.AddGeneratedReference(new CadReference(drawing, new CadHandle("000b")));
        sameElement.AddElement(third);
        Require(!ModelReadinessAnalyzer.Analyze(sameElement).IsReady, "source/generated ownership collision on one element must fail readiness");

        var clean = new SemanticProject(ProjectId.New(), "Clean Ownership");
        clean.AddFamily(family);
        var cleanElement = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W4", family.Id);
        cleanElement.SetSource(new CadReference(drawing, new CadHandle("C")));
        clean.AddElement(cleanElement);
        Require(ModelReadinessAnalyzer.Analyze(clean).IsReady, "unique CAD ownership should remain ready");

        Console.WriteLine("PASS canonical semantic CAD ownership health");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}

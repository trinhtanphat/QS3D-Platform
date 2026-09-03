using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleCadSourceProvenanceModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall Family");
        var project = new SemanticProject(ProjectId.New(), "Schedule CAD source provenance");
        project.AddFamily(family);
        var element = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "Wall", family.Id);
        var source = new CadReference(
            new DrawingId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            new CadHandle("000abc"));
        element.SetSource(source);
        project.AddElement(element);

        var fact = new QuantityFact(
            element.Id,
            "WALL.AREA",
            new QuantityValue(QuantityDimension.Area, 4d),
            source);
        var row = QuantityScheduleProjector.Project(project, new[] { fact }).Rows.Single();
        Equal(source, row.SourceReference!.Value);
        Equal("ABC", row.SourceReference.Value.Handle.Value);

        var legacyRow = new QuantityScheduleRow(
            ElementId.New(), "Legacy", SemanticElementKind.Wall, FamilyId.New(), "Wall Family", null, null,
            new[] { new QuantitySummary("WALL.AREA", QuantityDimension.Area, 1d, 1, 1) });
        if (legacyRow.SourceReference.HasValue)
            throw new InvalidOperationException("Legacy QuantityScheduleRow constructor must preserve source-less behavior.");

        Throws<ArgumentException>(() => new QuantityScheduleRow(
            ElementId.New(), "Bad drawing", SemanticElementKind.Wall, FamilyId.New(), "Wall Family", null, null,
            new[] { new QuantitySummary("WALL.AREA", QuantityDimension.Area, 1d, 1, 1) },
            new CadReference(new DrawingId(Guid.Empty), new CadHandle("A"))));
        Throws<ArgumentException>(() => new QuantityScheduleRow(
            ElementId.New(), "Bad handle", SemanticElementKind.Wall, FamilyId.New(), "Wall Family", null, null,
            new[] { new QuantitySummary("WALL.AREA", QuantityDimension.Area, 1d, 1, 1) },
            new CadReference(DrawingId.New(), default)));

        Console.WriteLine("PASS quantity schedule CAD source provenance");
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected '{expected}' but got '{actual}'.");
    }

    private static void Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}

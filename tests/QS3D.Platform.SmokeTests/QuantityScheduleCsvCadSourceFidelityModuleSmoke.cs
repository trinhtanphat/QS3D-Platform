using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleCsvCadSourceFidelityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var family = new Family(
            new FamilyId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            SemanticElementKind.Wall,
            "Wall Family");
        var project = new SemanticProject(
            new ProjectId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            "CAD source fidelity");
        project.AddFamily(family);

        var measuredSource = new CadReference(
            new DrawingId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            new CadHandle("000abc"));
        var emptySource = new CadReference(
            new DrawingId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
            new CadHandle("000def"));

        var measured = new SemanticElement(
            new ElementId(Guid.Parse("00000000-0000-0000-0000-000000000001")),
            SemanticElementKind.Wall,
            "Measured Wall",
            family.Id);
        measured.SetSource(measuredSource);
        project.AddElement(measured);

        var empty = new SemanticElement(
            new ElementId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            SemanticElementKind.Wall,
            "Empty Wall",
            family.Id);
        empty.SetSource(emptySource);
        project.AddElement(empty);

        var fact = new QuantityFact(
            measured.Id,
            "WALL.AREA",
            new QuantityValue(QuantityDimension.Area, 12.5d),
            measuredSource);
        var schedule = QuantityScheduleProjector.Project(project, new[] { fact }, includeElementsWithoutQuantities: true);
        var csv = QuantityScheduleCsv.Write(schedule);
        var lines = csv.Split(new[] { "\r\n" }, StringSplitOptions.None);

        const string expectedHeader = "ElementId,ElementName,Code,Dimension,Value,CanonicalUnit,ElementKind,FamilyId,FamilyName,FloorId,ZoneId,FactCount,ElementCount,SourceDrawingId,SourceHandle";
        Equal(expectedHeader, lines[0]);
        if (!lines[1].EndsWith(",1,1,aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa,ABC", StringComparison.Ordinal))
            throw new InvalidOperationException("Quantity schedule CSV lost the validated CAD source of a populated row.");
        if (!lines[2].EndsWith(",,,,bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb,DEF", StringComparison.Ordinal))
            throw new InvalidOperationException("Quantity schedule CSV lost CAD source provenance for an intentionally empty row.");

        Console.WriteLine("PASS quantity schedule CSV CAD source fidelity");
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected '{expected}' but got '{actual}'.");
    }
}

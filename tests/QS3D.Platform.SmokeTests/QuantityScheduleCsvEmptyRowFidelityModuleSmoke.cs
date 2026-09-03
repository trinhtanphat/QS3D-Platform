using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleCsvEmptyRowFidelityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        const string header = "ElementId,ElementName,Code,Dimension,Value,CanonicalUnit,ElementKind,FamilyId,FamilyName,FloorId,ZoneId\r\n";
        var family = new Family(new FamilyId(Guid.Parse("11111111-1111-1111-1111-111111111111")), SemanticElementKind.Wall, "Wall Family");
        var project = new SemanticProject(new ProjectId(Guid.Parse("22222222-2222-2222-2222-222222222222")), "CSV empty row fidelity");
        project.AddFamily(family);

        var emptyElement = new SemanticElement(
            new ElementId(Guid.Parse("00000000-0000-0000-0000-000000000001")),
            SemanticElementKind.Wall,
            "=EMPTY-WALL",
            family.Id);
        var populatedElement = new SemanticElement(
            new ElementId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            SemanticElementKind.Wall,
            "Measured Wall",
            family.Id);
        project.AddElement(emptyElement);
        project.AddElement(populatedElement);

        var fact = new QuantityFact(
            populatedElement.Id,
            "WALL.AREA",
            new QuantityValue(QuantityDimension.Area, 12.5d),
            populatedElement.SourceReference);
        var schedule = QuantityScheduleProjector.Project(project, new[] { fact }, includeElementsWithoutQuantities: true);
        Equal(2, schedule.Rows.Count);
        Equal(0, schedule.Rows.Single(row => row.ElementId == emptyElement.Id).Quantities.Count);

        var csv = QuantityScheduleCsv.Write(schedule);
        var lines = csv.Split(new[] { "\r\n" }, StringSplitOptions.None);
        Equal(4, lines.Length); // header + 2 data rows + terminal empty split item
        Equal("ElementId,ElementName,Code,Dimension,Value,CanonicalUnit,ElementKind,FamilyId,FamilyName,FloorId,ZoneId", lines[0]);
        Equal("00000000-0000-0000-0000-000000000001,'=EMPTY-WALL,,,,,Wall,11111111-1111-1111-1111-111111111111,Wall Family,,", lines[1]);
        Equal("00000000-0000-0000-0000-000000000002,Measured Wall,WALL.AREA,Area,12.5,m2,Wall,11111111-1111-1111-1111-111111111111,Wall Family,,", lines[2]);
        Equal(string.Empty, lines[3]);

        var emptyScheduleCsv = QuantityScheduleCsv.Write(new QuantitySchedule(Array.Empty<QuantityScheduleRow>()));
        Equal(header, emptyScheduleCsv);

        Console.WriteLine("PASS quantity schedule CSV empty-row fidelity");
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected '{expected}' but got '{actual}'.");
    }
}

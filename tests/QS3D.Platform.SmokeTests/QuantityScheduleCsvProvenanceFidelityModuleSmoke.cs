using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleCsvProvenanceFidelityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var familyId = new FamilyId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var floorId = new FloorId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var zoneId = new ZoneId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var row = new QuantityScheduleRow(
            new ElementId(Guid.Parse("00000000-0000-0000-0000-000000000001")),
            "Measured Wall",
            SemanticElementKind.Wall,
            familyId,
            "=Wall Family",
            floorId,
            zoneId,
            new[] { new QuantitySummary("WALL.AREA", QuantityDimension.Area, 12.5d, 1, 1) });

        var csv = QuantityScheduleCsv.Write(new QuantitySchedule(new[] { row }));
        var lines = csv.Split(new[] { "\r\n" }, StringSplitOptions.None);

        Equal(
            "ElementId,ElementName,Code,Dimension,Value,CanonicalUnit,ElementKind,FamilyId,FamilyName,FloorId,ZoneId,FactCount,ElementCount,SourceDrawingId,SourceHandle",
            lines[0]);
        Equal(
            "00000000-0000-0000-0000-000000000001,Measured Wall,WALL.AREA,Area,12.5,m2,Wall,11111111-1111-1111-1111-111111111111,'=Wall Family,22222222-2222-2222-2222-222222222222,33333333-3333-3333-3333-333333333333,1,1,,",
            lines[1]);
        Equal(string.Empty, lines[2]);

        var emptyRow = new QuantityScheduleRow(
            new ElementId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            "Empty Wall",
            SemanticElementKind.Wall,
            familyId,
            "Wall Family",
            null,
            null,
            Array.Empty<QuantitySummary>());
        var emptyCsv = QuantityScheduleCsv.Write(new QuantitySchedule(new[] { emptyRow }));
        var emptyLines = emptyCsv.Split(new[] { "\r\n" }, StringSplitOptions.None);
        Equal(
            "00000000-0000-0000-0000-000000000002,Empty Wall,,,,,Wall,11111111-1111-1111-1111-111111111111,Wall Family,,,,,,",
            emptyLines[1]);

        Console.WriteLine("PASS quantity schedule CSV provenance fidelity");
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected '{expected}' but got '{actual}'.");
    }
}

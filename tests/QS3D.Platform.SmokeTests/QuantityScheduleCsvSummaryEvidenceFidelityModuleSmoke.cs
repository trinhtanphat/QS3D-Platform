using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleCsvSummaryEvidenceFidelityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var elementId = new ElementId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var familyId = new FamilyId(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var oneFactCsv = Write(elementId, familyId, factCount: 1);
        var twoFactCsv = Write(elementId, familyId, factCount: 2);
        if (StringComparer.Ordinal.Equals(oneFactCsv, twoFactCsv))
            throw new InvalidOperationException("Quantity schedule CSV collapsed distinct summary evidence cardinalities to identical output.");

        var expectedHeader = "ElementId,ElementName,Code,Dimension,Value,CanonicalUnit,ElementKind,FamilyId,FamilyName,FloorId,ZoneId,FactCount,ElementCount\r\n";
        if (!oneFactCsv.StartsWith(expectedHeader, StringComparison.Ordinal))
            throw new InvalidOperationException("Quantity schedule CSV did not append summary evidence columns after the existing compatibility prefix.");
        if (!oneFactCsv.Contains(",1,1\r\n", StringComparison.Ordinal))
            throw new InvalidOperationException("Quantity schedule CSV did not preserve one-fact summary evidence cardinality.");
        if (!twoFactCsv.Contains(",2,1\r\n", StringComparison.Ordinal))
            throw new InvalidOperationException("Quantity schedule CSV did not preserve two-fact summary evidence cardinality.");

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
        if (!emptyLines[1].EndsWith(",,", StringComparison.Ordinal))
            throw new InvalidOperationException("Quantity schedule CSV must leave summary evidence fields blank for an intentionally empty row.");

        Console.WriteLine("PASS quantity schedule CSV summary evidence fidelity");
    }

    private static string Write(ElementId elementId, FamilyId familyId, int factCount)
    {
        var row = new QuantityScheduleRow(
            elementId,
            "Measured Wall",
            SemanticElementKind.Wall,
            familyId,
            "Wall Family",
            null,
            null,
            new[] { new QuantitySummary("WALL.AREA", QuantityDimension.Area, 12.5d, factCount, 1) });
        return QuantityScheduleCsv.Write(new QuantitySchedule(new[] { row }));
    }
}

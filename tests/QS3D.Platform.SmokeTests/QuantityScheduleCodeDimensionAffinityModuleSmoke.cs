using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleCodeDimensionAffinityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var failures = new List<string>();
        VerifySameCodeDifferentDimensionRejected(failures);
        VerifyDistinctCodesRemainValid(failures);

        if (failures.Count != 0)
            throw new InvalidOperationException("Quantity schedule code/dimension affinity failed: " + string.Join("; ", failures));

        Console.WriteLine("PASS quantity schedule code dimension affinity");
    }

    private static void VerifySameCodeDifferentDimensionRejected(List<string> failures)
    {
        var quantities = new[]
        {
            new QuantitySummary("WALL.QTY", QuantityDimension.Length, 3d, 1, 1),
            new QuantitySummary("WALL.QTY", QuantityDimension.Area, 9d, 1, 1)
        };

        try
        {
            _ = CreateRow(quantities);
            failures.Add("same code with different dimensions was accepted");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Quantity code 'WALL.QTY'", StringComparison.Ordinal))
        {
        }
        catch (Exception ex)
        {
            failures.Add("same-code/different-dimension rejection threw unexpected " + ex.GetType().Name);
        }
    }

    private static void VerifyDistinctCodesRemainValid(List<string> failures)
    {
        var row = CreateRow(new[]
        {
            new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 3d, 1, 1),
            new QuantitySummary("WALL.AREA", QuantityDimension.Area, 9d, 1, 1)
        });

        if (row.Quantities.Count != 2
            || row.Quantities[0].Code != "WALL.AREA"
            || row.Quantities[1].Code != "WALL.LENGTH")
            failures.Add("valid distinct quantity codes lost canonical ordering");
    }

    private static QuantityScheduleRow CreateRow(IEnumerable<QuantitySummary> quantities) =>
        new(
            new ElementId(Guid.Parse("11111111-2222-3333-4444-555555555555")),
            "Wall 1",
            SemanticElementKind.Wall,
            new FamilyId(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            "Wall family",
            null,
            null,
            quantities);
}

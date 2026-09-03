using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqCodeDimensionAffinityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var failures = new List<string>();
        VerifyDirectProjectionRejectsAmbiguousCode(failures);
        VerifyProjectorRejectsAmbiguousQuantityCode(failures);
        VerifyProjectorRejectsAmbiguousRateCode(failures);
        VerifyDistinctCodesRemainValid(failures);

        if (failures.Count != 0)
            throw new InvalidOperationException("BOQ code/dimension affinity failed: " + string.Join("; ", failures));

        Console.WriteLine("PASS BOQ code dimension affinity");
    }

    private static void VerifyDirectProjectionRejectsAmbiguousCode(List<string> failures)
    {
        var lines = new[]
        {
            new BoqLine("WALL.QTY", new QuantityValue(QuantityDimension.Length, 2d), 1, 10m, new Money(20m, "USD")),
            new BoqLine("WALL.QTY", new QuantityValue(QuantityDimension.Area, 3d), 1, 10m, new Money(30m, "USD"))
        };

        ExpectInvalidOperation(
            () => _ = new BoqProjection(lines, "USD"),
            "BQ code 'WALL.QTY'",
            "direct BOQ projection accepted same code in multiple dimensions",
            failures);
    }

    private static void VerifyProjectorRejectsAmbiguousQuantityCode(List<string> failures)
    {
        var quantities = new[]
        {
            new QuantitySummary("WALL.QTY", QuantityDimension.Length, 2d, 1, 1),
            new QuantitySummary("WALL.QTY", QuantityDimension.Area, 3d, 1, 1)
        };
        var rates = new[]
        {
            new UnitRate("WALL.QTY", QuantityDimension.Length, 10m, "USD"),
            new UnitRate("WALL.QTY", QuantityDimension.Area, 20m, "USD")
        };

        ExpectInvalidOperation(
            () => _ = BoqProjector.Project(quantities, rates, "USD"),
            "Rate code 'WALL.QTY'",
            "BOQ projector accepted same rate code in multiple dimensions before quantity validation",
            failures);
    }

    private static void VerifyProjectorRejectsAmbiguousRateCode(List<string> failures)
    {
        var quantities = new[]
        {
            new QuantitySummary("WALL.QTY", QuantityDimension.Length, 2d, 1, 1)
        };
        var rates = new[]
        {
            new UnitRate("WALL.QTY", QuantityDimension.Length, 10m, "USD"),
            new UnitRate("WALL.QTY", QuantityDimension.Area, 20m, "USD")
        };

        ExpectInvalidOperation(
            () => _ = BoqProjector.Project(quantities, rates, "USD"),
            "Rate code 'WALL.QTY'",
            "BOQ projector accepted same rate code in multiple dimensions",
            failures);
    }

    private static void VerifyDistinctCodesRemainValid(List<string> failures)
    {
        var quantities = new[]
        {
            new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 2d, 1, 1),
            new QuantitySummary("WALL.AREA", QuantityDimension.Area, 3d, 1, 1)
        };
        var rates = new[]
        {
            new UnitRate("WALL.LENGTH", QuantityDimension.Length, 10m, "USD"),
            new UnitRate("WALL.AREA", QuantityDimension.Area, 20m, "USD")
        };

        var projection = BoqProjector.Project(quantities, rates, "USD");
        if (projection.Lines.Count != 2 || projection.Total.Amount != 80m)
            failures.Add("valid distinct BOQ codes changed behavior");
    }

    private static void ExpectInvalidOperation(Action action, string expectedPrefix, string failureName, List<string> failures)
    {
        try
        {
            action();
            failures.Add(failureName);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
        }
        catch (Exception ex)
        {
            failures.Add(failureName + " threw unexpected " + ex.GetType().Name);
        }
    }
}

using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqZeroRateDecimalRangeModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyProjectorAcceptsZeroRateOutsideDecimalRange(double.MaxValue, "HUGE");
        VerifyProjectorAcceptsZeroRateOutsideDecimalRange(double.Epsilon, "TINY");
        VerifyDirectProjectionAcceptsZeroRateOutsideDecimalRange(double.MaxValue, "DIRECT.HUGE");
        VerifyDirectProjectionAcceptsZeroRateOutsideDecimalRange(double.Epsilon, "DIRECT.TINY");
        VerifyPositiveRateStillRequiresExactDecimalQuantity(double.MaxValue, "POSITIVE.HUGE");
        VerifyPositiveRateStillRequiresExactDecimalQuantity(double.Epsilon, "POSITIVE.TINY");
        Console.WriteLine("PASS BOQ zero-rate decimal-range short-circuit");
    }

    private static void VerifyProjectorAcceptsZeroRateOutsideDecimalRange(double quantity, string code)
    {
        var summary = new QuantitySummary(code, QuantityDimension.Mass, quantity, factCount: 1, elementCount: 1);
        var rate = new UnitRate(code, QuantityDimension.Mass, 0m, "USD");

        var projection = BoqProjector.Project(new[] { summary }, new[] { rate }, "USD");
        var line = projection.Lines.Single();
        if (line.Total.Amount != 0m || projection.Total.Amount != 0m)
            throw new InvalidOperationException($"Zero-rate BOQ '{code}' must total exactly zero.");
    }

    private static void VerifyDirectProjectionAcceptsZeroRateOutsideDecimalRange(double quantity, string code)
    {
        var line = new BoqLine(
            code,
            new QuantityValue(QuantityDimension.Mass, quantity),
            elementCount: 1,
            unitRate: 0m,
            new Money(0m, "USD"));

        var projection = new BoqProjection(new[] { line }, "USD");
        if (projection.Total.Amount != 0m)
            throw new InvalidOperationException($"Direct zero-rate BOQ '{code}' must total exactly zero.");
    }

    private static void VerifyPositiveRateStillRequiresExactDecimalQuantity(double quantity, string code)
    {
        var summary = new QuantitySummary(code, QuantityDimension.Mass, quantity, factCount: 1, elementCount: 1);
        var rate = new UnitRate(code, QuantityDimension.Mass, 1m, "USD");
        try
        {
            _ = BoqProjector.Project(new[] { summary }, new[] { rate }, "USD");
            throw new InvalidOperationException($"Positive-rate BOQ '{code}' accepted a quantity that cannot round-trip exactly through decimal.");
        }
        catch (OverflowException ex) when (ex.Message.StartsWith($"Quantity '{code}' cannot be represented as decimal", StringComparison.Ordinal))
        {
        }
    }
}

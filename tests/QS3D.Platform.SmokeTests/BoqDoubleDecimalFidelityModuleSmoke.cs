using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqDoubleDecimalFidelityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        const double source = 1.2345678901234567d;
        var summary = new QuantitySummary("VOL", QuantityDimension.Volume, source, factCount: 1, elementCount: 1);
        var rate = new UnitRate("VOL", QuantityDimension.Volume, 1m, "USD");

        var projection = BoqProjector.Project(new[] { summary }, new[] { rate }, "USD");
        var expected = decimal.Parse(
            source.ToString("R", CultureInfo.InvariantCulture),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);

        if (projection.Total.Amount != expected)
        {
            throw new InvalidOperationException(
                $"BOQ decimal projection changed canonical quantity evidence from {source.ToString("R", CultureInfo.InvariantCulture)} to {projection.Total.Amount.ToString(CultureInfo.InvariantCulture)}.");
        }

        Console.WriteLine("PASS BOQ double/decimal round-trip fidelity");
    }
}

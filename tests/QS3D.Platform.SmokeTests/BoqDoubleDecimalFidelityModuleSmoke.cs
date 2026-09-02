using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqDoubleDecimalFidelityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyRoundTripPrecision();
        VerifyNonzeroUnderflowFailsClosed();
        Console.WriteLine("PASS BOQ double/decimal round-trip fidelity");
    }

    private static void VerifyRoundTripPrecision()
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
    }

    private static void VerifyNonzeroUnderflowFailsClosed()
    {
        var summary = new QuantitySummary("LEN", QuantityDimension.Length, double.Epsilon, factCount: 1, elementCount: 1);
        var rate = new UnitRate("LEN", QuantityDimension.Length, 1m, "USD");

        try
        {
            var projection = BoqProjector.Project(new[] { summary }, new[] { rate }, "USD");
            throw new InvalidOperationException(
                $"Nonzero quantity underflow was accepted as {projection.Total.Amount.ToString(CultureInfo.InvariantCulture)} USD.");
        }
        catch (OverflowException ex) when (ex.Message.StartsWith("Quantity 'LEN' cannot be represented as decimal", StringComparison.Ordinal))
        {
        }
    }
}

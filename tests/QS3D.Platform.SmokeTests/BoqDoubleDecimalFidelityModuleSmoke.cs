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
        VerifySubDecimalRoundingFailsClosed();
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
        ExpectUnrepresentable("LEN", QuantityDimension.Length, double.Epsilon);
    }

    private static void VerifySubDecimalRoundingFailsClosed()
    {
        ExpectUnrepresentable("AREA", QuantityDimension.Area, 9e-29d);
    }

    private static void ExpectUnrepresentable(string code, QuantityDimension dimension, double source)
    {
        var summary = new QuantitySummary(code, dimension, source, factCount: 1, elementCount: 1);
        var rate = new UnitRate(code, dimension, 1m, "USD");

        try
        {
            var projection = BoqProjector.Project(new[] { summary }, new[] { rate }, "USD");
            throw new InvalidOperationException(
                $"Unrepresentable quantity {source.ToString("R", CultureInfo.InvariantCulture)} was accepted as {projection.Total.Amount.ToString(CultureInfo.InvariantCulture)} USD.");
        }
        catch (OverflowException ex) when (ex.Message.StartsWith($"Quantity '{code}' cannot be represented as decimal", StringComparison.Ordinal))
        {
        }
    }
}

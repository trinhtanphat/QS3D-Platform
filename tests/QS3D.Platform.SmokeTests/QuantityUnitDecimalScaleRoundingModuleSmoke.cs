using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityUnitDecimalScaleRoundingModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        AssertBits(
            QuantityUnits.ToCanonical(69789978031.23123d, QuantityUnit.Millimeter),
            0x4190A3A4681FFB14L,
            "Millimeter conversion must round the exact decimal division once.");
        AssertBits(
            QuantityUnits.ToCanonical(3.75015129991706e182d, QuantityUnit.Centimeter),
            0x656A620021E1E8AFL,
            "Centimeter conversion must round the exact decimal division once.");
        AssertBits(
            QuantityUnits.ToCanonical(6.35142695953089e39d, QuantityUnit.SquareMillimeter),
            0x46F5C893F0D2DA5BL,
            "Square-millimeter conversion must round the exact decimal division once.");
        AssertBits(
            QuantityUnits.ToCanonical(1.1012848168869247e-144d, QuantityUnit.SquareCentimeter),
            0x21351FF2C7A30571L,
            "Square-centimeter conversion must round the exact decimal division once.");
        AssertBits(
            QuantityUnits.ToCanonical(2.7402992211211728e-154d, QuantityUnit.CubicMillimeter),
            0x1E2F1C43EA284B7EL,
            "Cubic-millimeter conversion must round the exact decimal division once.");
        AssertBits(
            QuantityUnits.ToCanonical(5.862813159411555e86d, QuantityUnit.CubicCentimeter),
            0x50B3E4D493C69A3FL,
            "Cubic-centimeter conversion must round the exact decimal division once.");
        AssertBits(
            QuantityUnits.ToCanonical(1.836449551960462e85d, QuantityUnit.Gram),
            0x5104E856936C4C53L,
            "Gram conversion must round the exact decimal division once.");

        AssertBits(
            QuantityUnits.FromCanonical(4.824343610079485e-138d, QuantityUnit.Millimeter),
            0x240C0D5A3AB7EF51L,
            "Canonical-to-millimeter conversion must round the exact decimal multiplication once.");
        AssertBits(
            QuantityUnits.FromCanonical(2.3394716680708133e-75d, QuantityUnit.Centimeter),
            0x30DA744ECC3D1D7AL,
            "Canonical-to-centimeter conversion must round the exact decimal multiplication once.");
        AssertBits(
            QuantityUnits.FromCanonical(3.2878844613665573e-44d, QuantityUnit.SquareMillimeter),
            0x3826604D78E8D755L,
            "Canonical-to-square-millimeter conversion must round the exact decimal multiplication once.");
        AssertBits(
            QuantityUnits.FromCanonical(1.8821237802971546e189d, QuantityUnit.SquareCentimeter),
            0x681080433D440299L,
            "Canonical-to-square-centimeter conversion must round the exact decimal multiplication once.");
        AssertBits(
            QuantityUnits.FromCanonical(2.032398607522834e156d, QuantityUnit.CubicMillimeter),
            0x6241A58823369C56L,
            "Canonical-to-cubic-millimeter conversion must round the exact decimal multiplication once.");
        AssertBits(
            QuantityUnits.FromCanonical(86818797615724.81d, QuantityUnit.CubicCentimeter),
            0x4412D36951C87F3BL,
            "Canonical-to-cubic-centimeter conversion must round the exact decimal multiplication once.");
        AssertBits(
            QuantityUnits.FromCanonical(3.3164784855453967e-203d, QuantityUnit.Gram),
            0x16844F0D9B6EA68EL,
            "Canonical-to-gram conversion must round the exact decimal multiplication once.");

        AssertEqual(1d, QuantityUnits.ToCanonical(1000d, QuantityUnit.Millimeter), "1000 mm");
        AssertEqual(1d, QuantityUnits.ToCanonical(100d, QuantityUnit.Centimeter), "100 cm");
        AssertEqual(1d, QuantityUnits.ToCanonical(1_000_000d, QuantityUnit.SquareMillimeter), "1,000,000 mm2");
        AssertEqual(1d, QuantityUnits.ToCanonical(1_000_000_000d, QuantityUnit.CubicMillimeter), "1,000,000,000 mm3");
        AssertEqual(1d, QuantityUnits.ToCanonical(1000d, QuantityUnit.Gram), "1000 g");
        AssertEqual(2000d, QuantityUnits.ToCanonical(2d, QuantityUnit.Tonne), "2 t");
        AssertEqual(2d, QuantityUnits.FromCanonical(2000d, QuantityUnit.Tonne), "2000 kg as tonnes");

        AssertPositiveZero(QuantityUnits.ToCanonical(-0d, QuantityUnit.Millimeter), "ToCanonical negative zero");
        AssertPositiveZero(QuantityUnits.FromCanonical(-0d, QuantityUnit.Millimeter), "FromCanonical negative zero");

        AssertOverflow(() => QuantityUnits.ToCanonical(double.MaxValue, QuantityUnit.Tonne), "tonne conversion overflow");
        AssertOverflow(() => QuantityUnits.FromCanonical(double.MaxValue, QuantityUnit.Millimeter), "millimeter conversion overflow");
        AssertOverflow(() => QuantityUnits.ToCanonical(double.Epsilon, QuantityUnit.CubicMillimeter), "cubic-millimeter conversion underflow");
        AssertOverflow(() => QuantityUnits.FromCanonical(double.Epsilon, QuantityUnit.Tonne), "tonne conversion underflow");

        Console.WriteLine("PASS quantity unit exact decimal scale rounding");
    }

    private static void AssertBits(double actual, long expectedBits, string message)
    {
        var actualBits = BitConverter.DoubleToInt64Bits(actual);
        if (actualBits != expectedBits)
            throw new InvalidOperationException($"{message} Expected bits 0x{expectedBits:X16}, got 0x{actualBits:X16} ({actual:R}).");
    }

    private static void AssertEqual(double expected, double actual, string message)
    {
        if (BitConverter.DoubleToInt64Bits(actual) != BitConverter.DoubleToInt64Bits(expected))
            throw new InvalidOperationException($"{message} expected {expected:R}, got {actual:R}.");
    }

    private static void AssertPositiveZero(double actual, string message)
    {
        if (BitConverter.DoubleToInt64Bits(actual) != 0L)
            throw new InvalidOperationException($"{message} must normalize to positive zero.");
    }

    private static void AssertOverflow(Func<double> action, string message)
    {
        try
        {
            var result = action();
            throw new InvalidOperationException($"{message} must fail closed, but returned {result:R}.");
        }
        catch (OverflowException)
        {
        }
    }
}

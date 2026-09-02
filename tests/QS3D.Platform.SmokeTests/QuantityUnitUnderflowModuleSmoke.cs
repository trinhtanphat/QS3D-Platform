using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityUnitUnderflowModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        AssertUnderflow(
            () => QuantityUnits.ToCanonical(double.Epsilon, QuantityUnit.Millimeter),
            "Positive millimeter quantity underflowed silently to zero canonical meters.");
        AssertUnderflow(
            () => QuantityUnits.FromCanonical(double.Epsilon, QuantityUnit.Tonne),
            "Positive canonical mass underflowed silently to zero tonnes.");

        if (QuantityUnits.ToCanonical(0d, QuantityUnit.Millimeter) != 0d)
            throw new InvalidOperationException("Exact zero millimeters must remain exact zero canonical meters.");
        if (QuantityUnits.FromCanonical(0d, QuantityUnit.Tonne) != 0d)
            throw new InvalidOperationException("Exact zero canonical mass must remain exact zero tonnes.");

        Console.WriteLine("PASS quantity unit conversion underflow safety");
    }

    private static void AssertUnderflow(Func<double> action, string message)
    {
        try
        {
            var result = action();
            throw new InvalidOperationException($"{message} Returned {result:R} instead of failing closed.");
        }
        catch (OverflowException)
        {
        }
    }
}

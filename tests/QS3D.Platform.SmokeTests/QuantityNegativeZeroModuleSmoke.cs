using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityNegativeZeroModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var failures = new List<string>();
        const double negativeZero = -0d;

        RequirePositiveZero(failures, "QuantityValue", new QuantityValue(QuantityDimension.Length, negativeZero).Value);
        RequirePositiveZero(failures, "ToCanonical", QuantityUnits.ToCanonical(negativeZero, QuantityUnit.Millimeter));
        RequirePositiveZero(failures, "FromCanonical", QuantityUnits.FromCanonical(negativeZero, QuantityUnit.Tonne));
        RequirePositiveZero(failures, "ToQuantityValue", QuantityUnits.ToQuantityValue(negativeZero, QuantityUnit.Gram).Value);

        var ordinary = QuantityUnits.ToCanonical(12.5d, QuantityUnit.Centimeter);
        if (ordinary != 0.125d)
            failures.Add("ordinary centimeter conversion changed");

        var explicitPositiveZero = new QuantityValue(QuantityDimension.Volume, 0d);
        RequirePositiveZero(failures, "positive zero", explicitPositiveZero.Value);

        if (failures.Count != 0)
            throw new InvalidOperationException("Quantity negative-zero canonicalization failed: " + string.Join("; ", failures));

        Console.WriteLine("PASS quantity negative-zero canonicalization");
    }

    private static void RequirePositiveZero(List<string> failures, string scenario, double value)
    {
        if (value != 0d)
        {
            failures.Add(scenario + " did not produce zero");
            return;
        }

        if (BitConverter.DoubleToInt64Bits(value) != 0L)
            failures.Add(scenario + " preserved a negative-zero sign bit");
    }
}

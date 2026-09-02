using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantitySummaryProvenanceModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var failures = new List<string>();

        ExpectRejected(failures, "element count exceeds fact count", () =>
            new QuantitySummary("VOL", QuantityDimension.Volume, 1d, factCount: 1, elementCount: 2));
        ExpectRejected(failures, "zero facts with nonzero element count", () =>
            new QuantitySummary("VOL", QuantityDimension.Volume, 0d, factCount: 0, elementCount: 1));
        ExpectRejected(failures, "zero facts with nonzero quantity", () =>
            new QuantitySummary("VOL", QuantityDimension.Volume, 1d, factCount: 0, elementCount: 0));
        ExpectRejected(failures, "facts without any source element", () =>
            new QuantitySummary("VOL", QuantityDimension.Volume, 0d, factCount: 1, elementCount: 0));

        ExpectAccepted(failures, "empty zero summary", () =>
            new QuantitySummary("VOL", QuantityDimension.Volume, 0d, factCount: 0, elementCount: 0));
        ExpectAccepted(failures, "zero-valued summary backed by facts", () =>
            new QuantitySummary("VOL", QuantityDimension.Volume, 0d, factCount: 2, elementCount: 1));
        ExpectAccepted(failures, "normal positive summary", () =>
            new QuantitySummary("VOL", QuantityDimension.Volume, 12.5d, factCount: 3, elementCount: 2));

        if (failures.Count != 0)
            throw new InvalidOperationException("QuantitySummary provenance contract failed: " + string.Join("; ", failures));

        Console.WriteLine("PASS QuantitySummary provenance invariants");
    }

    private static void ExpectRejected(List<string> failures, string scenario, Action action)
    {
        try
        {
            action();
            failures.Add(scenario + " was accepted");
        }
        catch (ArgumentException)
        {
        }
        catch (Exception ex)
        {
            failures.Add(scenario + " threw unexpected " + ex.GetType().Name);
        }
    }

    private static void ExpectAccepted(List<string> failures, string scenario, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            failures.Add(scenario + " threw " + ex.GetType().Name);
        }
    }
}

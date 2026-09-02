using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqElementProvenanceModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var failures = new List<string>();

        ExpectRejected(failures, "positive quantity with zero elements", () =>
            new BoqLine(
                "VOL",
                new QuantityValue(QuantityDimension.Volume, 2d),
                elementCount: 0,
                unitRate: 10m,
                new Money(20m, "USD")));

        ExpectAccepted(failures, "empty zero line", () =>
        {
            var line = new BoqLine(
                "VOL",
                new QuantityValue(QuantityDimension.Volume, 0d),
                elementCount: 0,
                unitRate: 10m,
                new Money(0m, "USD"));
            var projection = new BoqProjection(new[] { line }, "USD");
            if (projection.Total.Amount != 0m)
                failures.Add("empty zero line changed projection total");
        });

        ExpectAccepted(failures, "zero quantity backed by elements", () =>
        {
            var line = new BoqLine(
                "VOL",
                new QuantityValue(QuantityDimension.Volume, 0d),
                elementCount: 2,
                unitRate: 10m,
                new Money(0m, "USD"));
            _ = new BoqProjection(new[] { line }, "USD");
        });

        ExpectAccepted(failures, "positive quantity backed by an element", () =>
        {
            var line = new BoqLine(
                "VOL",
                new QuantityValue(QuantityDimension.Volume, 2d),
                elementCount: 1,
                unitRate: 10m,
                new Money(20m, "USD"));
            _ = new BoqProjection(new[] { line }, "USD");
        });

        if (failures.Count != 0)
            throw new InvalidOperationException("BOQ element provenance contract failed: " + string.Join("; ", failures));

        Console.WriteLine("PASS BOQ element provenance invariants");
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

using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqDirectLineIntegrityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var failures = new List<string>();
        VerifyStaleTotalRejected(failures);
        VerifyUnrepresentableQuantityRejected(failures);
        VerifyDirectMultiplicationOverflowRejected(failures);

        if (failures.Count != 0)
            throw new InvalidOperationException("Direct BOQ line integrity gap: " + string.Join("; ", failures));

        Console.WriteLine("PASS direct BOQ line arithmetic integrity");
    }

    private static void VerifyStaleTotalRejected(List<string> failures)
    {
        var line = new BoqLine(
            "VOL",
            new QuantityValue(QuantityDimension.Volume, 2d),
            1,
            10m,
            new Money(999m, "USD"));

        ExpectInvalidOperation(
            () => _ = new BoqProjection(new[] { line }, "USD"),
            "BQ line total mismatch",
            "stale 999 total accepted for 2 × 10",
            failures);
    }

    private static void VerifyUnrepresentableQuantityRejected(List<string> failures)
    {
        var line = new BoqLine(
            "LEN",
            new QuantityValue(QuantityDimension.Length, double.Epsilon),
            1,
            1m,
            new Money(0m, "USD"));

        ExpectOverflow(
            () => _ = new BoqProjection(new[] { line }, "USD"),
            "Quantity 'LEN' cannot be represented as decimal",
            "unrepresentable double quantity accepted directly",
            failures);
    }

    private static void VerifyDirectMultiplicationOverflowRejected(List<string> failures)
    {
        var line = new BoqLine(
            "MASS",
            new QuantityValue(QuantityDimension.Mass, 1e28d),
            1,
            10m,
            new Money(0m, "USD"));

        ExpectOverflow(
            () => _ = new BoqProjection(new[] { line }, "USD"),
            "Cost for 'MASS' exceeds decimal range",
            "direct quantity × rate overflow bypass accepted",
            failures);
    }

    private static void ExpectInvalidOperation(
        Action action,
        string messagePrefix,
        string failure,
        List<string> failures)
    {
        try
        {
            action();
            failures.Add(failure);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith(messagePrefix, StringComparison.Ordinal))
        {
        }
        catch (Exception ex)
        {
            failures.Add(failure + " threw unexpected " + ex.GetType().Name);
        }
    }

    private static void ExpectOverflow(
        Action action,
        string messagePrefix,
        string failure,
        List<string> failures)
    {
        try
        {
            action();
            failures.Add(failure);
        }
        catch (OverflowException ex) when (ex.Message.StartsWith(messagePrefix, StringComparison.Ordinal))
        {
        }
        catch (Exception ex)
        {
            failures.Add(failure + " threw unexpected " + ex.GetType().Name);
        }
    }
}

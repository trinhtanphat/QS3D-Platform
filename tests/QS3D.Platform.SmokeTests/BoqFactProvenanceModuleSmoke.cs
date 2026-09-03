using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqFactProvenanceModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var failures = new List<string>();

        var evidenceAware = new BoqLine(
            "AREA",
            new QuantityValue(QuantityDimension.Area, 12.5d),
            factCount: 2,
            elementCount: 1,
            unitRate: 4m,
            total: new Money(50m, "USD"));
        if (evidenceAware.FactCount != 2)
            failures.Add("evidence-aware BOQ line did not retain fact count");
        if (evidenceAware.ElementCount != 1)
            failures.Add("evidence-aware BOQ line changed element count");

        var legacy = new BoqLine(
            "AREA",
            new QuantityValue(QuantityDimension.Area, 12.5d),
            elementCount: 1,
            unitRate: 4m,
            total: new Money(50m, "USD"));
        if (legacy.FactCount is not null)
            failures.Add("legacy constructor fabricated fact provenance");

        ExpectRejected(failures, "fewer facts than elements", () =>
            new BoqLine(
                "AREA",
                new QuantityValue(QuantityDimension.Area, 12.5d),
                factCount: 1,
                elementCount: 2,
                unitRate: 4m,
                total: new Money(50m, "USD")));

        ExpectRejected(failures, "positive quantity with zero facts", () =>
            new BoqLine(
                "AREA",
                new QuantityValue(QuantityDimension.Area, 12.5d),
                factCount: 0,
                elementCount: 0,
                unitRate: 4m,
                total: new Money(50m, "USD")));

        var zeroBacked = new BoqLine(
            "AREA",
            new QuantityValue(QuantityDimension.Area, 0d),
            factCount: 2,
            elementCount: 1,
            unitRate: 4m,
            total: new Money(0m, "USD"));
        if (zeroBacked.FactCount != 2 || zeroBacked.ElementCount != 1)
            failures.Add("zero-valued fact-backed line lost evidence cardinality");

        var summary = new QuantitySummary(
            "AREA",
            QuantityDimension.Area,
            12.5d,
            factCount: 2,
            elementCount: 1);
        var projection = BoqProjector.Project(
            new[] { summary },
            new[] { new UnitRate("AREA", QuantityDimension.Area, 4m, "USD") },
            "USD");
        var projected = projection.Lines.Single();
        if (projected.FactCount != 2 || projected.ElementCount != 1)
            failures.Add("BOQ projector did not preserve summary fact/element evidence");
        if (projected.Total.Amount != 50m)
            failures.Add("fact provenance propagation changed commercial total");

        if (failures.Count != 0)
            throw new InvalidOperationException("BOQ fact provenance contract failed: " + string.Join("; ", failures));

        Console.WriteLine("PASS BOQ fact provenance invariants");
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
}

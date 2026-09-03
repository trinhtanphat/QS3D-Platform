using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityReadonlyCollectionExposureModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var firstSummary = new QuantitySummary("A.LENGTH", QuantityDimension.Length, 2d, 1, 1);
        var secondSummary = new QuantitySummary("B.LENGTH", QuantityDimension.Length, 3d, 1, 1);
        var row = new QuantityScheduleRow(
            ElementId.New(),
            "Wall",
            SemanticElementKind.Wall,
            FamilyId.New(),
            "Wall Family",
            null,
            null,
            new[] { secondSummary, firstSummary });

        AssertReadOnlyView(row.Quantities, "QuantityScheduleRow.Quantities");
        if (!ReferenceEquals(row.Quantities[0], firstSummary) || !ReferenceEquals(row.Quantities[1], secondSummary))
            throw new InvalidOperationException("QuantityScheduleRow.Quantities ordering or identity changed.");

        var schedule = new QuantitySchedule(new[] { row });
        AssertReadOnlyView(schedule.Rows, "QuantitySchedule.Rows");
        if (!ReferenceEquals(schedule.Rows[0], row))
            throw new InvalidOperationException("QuantitySchedule.Rows identity changed.");

        var factor = new QuantityFactor("Length", QuantityUnit.Meter);
        var rule = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "WALL.LENGTH",
            QuantityDimension.Length,
            new[] { factor });
        AssertReadOnlyView(rule.Factors, "QuantityRuleDefinition.Factors");
        if (!ReferenceEquals(rule.Factors[0], factor))
            throw new InvalidOperationException("QuantityRuleDefinition.Factors identity changed.");

        var catalog = new QuantityRuleCatalog(new[] { rule });
        AssertReadOnlyView(catalog.Rules, "QuantityRuleCatalog.Rules");
        AssertReadOnlyView(catalog.ForKind(SemanticElementKind.Wall), "QuantityRuleCatalog.ForKind");
        if (!ReferenceEquals(catalog.Rules[0], rule)
            || !ReferenceEquals(catalog.ForKind(SemanticElementKind.Wall)[0], rule))
            throw new InvalidOperationException("QuantityRuleCatalog ordering or identity changed.");

        Console.WriteLine("PASS quantity validated collections expose immutable read-only views");
    }

    private static void AssertReadOnlyView<T>(IReadOnlyList<T> values, string surface)
    {
        if (values is T[])
            throw new InvalidOperationException($"{surface} exposes its validated backing array.");

        if (values is IList<T> mutableView)
        {
            try
            {
                mutableView[0] = values[0];
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new InvalidOperationException($"{surface} permits mutation through IList<T>.");
        }
    }
}

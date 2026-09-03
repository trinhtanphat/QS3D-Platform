using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityAccumulatorCrossKeyProvenanceModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var elementId = ElementId.New();
        var source = new CadReference(DrawingId.New(), new CadHandle("31"));
        var otherSource = new CadReference(DrawingId.New(), new CadHandle("32"));

        ExpectRejected(
            new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 3d), source),
            new QuantityFact(elementId, "WALL.AREA", new QuantityValue(QuantityDimension.Area, 4d), otherSource),
            "different CAD references for the same element across quantity keys");
        ExpectRejected(
            new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 3d), source),
            new QuantityFact(elementId, "WALL.AREA", new QuantityValue(QuantityDimension.Area, 4d)),
            "null/non-null CAD provenance for the same element across quantity keys");

        var matching = QuantityAccumulator.Summarize(new[]
        {
            new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 3d), source),
            new QuantityFact(elementId, "WALL.AREA", new QuantityValue(QuantityDimension.Area, 4d), source)
        });
        AssertTwoSummaries(matching, "matching same-element provenance across quantity keys");

        var matchingNull = QuantityAccumulator.Summarize(new[]
        {
            new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 3d)),
            new QuantityFact(elementId, "WALL.AREA", new QuantityValue(QuantityDimension.Area, 4d))
        });
        AssertTwoSummaries(matchingNull, "matching null provenance across quantity keys");

        var differentElements = QuantityAccumulator.Summarize(new[]
        {
            new QuantityFact(ElementId.New(), "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 3d), source),
            new QuantityFact(ElementId.New(), "WALL.AREA", new QuantityValue(QuantityDimension.Area, 4d), otherSource)
        });
        AssertTwoSummaries(differentElements, "independent element provenance across quantity keys");

        Console.WriteLine("PASS quantity accumulator cross-key CAD provenance consistency enforced");
    }

    private static void ExpectRejected(QuantityFact first, QuantityFact second, string scenario)
    {
        try
        {
            QuantityAccumulator.Summarize(new[] { first, second });
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException($"Quantity accumulator accepted {scenario}.");
    }

    private static void AssertTwoSummaries(IReadOnlyList<QuantitySummary> summaries, string scenario)
    {
        if (summaries.Count != 2)
            throw new InvalidOperationException($"Quantity accumulator corrupted {scenario}: expected two summaries, got {summaries.Count}.");

        var length = summaries.SingleOrDefault(static summary => summary.Code == "WALL.LENGTH" && summary.Quantity.Dimension == QuantityDimension.Length);
        var area = summaries.SingleOrDefault(static summary => summary.Code == "WALL.AREA" && summary.Quantity.Dimension == QuantityDimension.Area);
        if (length is null || area is null
            || length.Quantity.Value != 3d || length.FactCount != 1 || length.ElementCount != 1
            || area.Quantity.Value != 4d || area.FactCount != 1 || area.ElementCount != 1)
            throw new InvalidOperationException($"Quantity accumulator corrupted {scenario}.");
    }
}

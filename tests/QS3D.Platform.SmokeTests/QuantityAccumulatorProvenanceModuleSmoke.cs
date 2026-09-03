using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityAccumulatorProvenanceModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var elementId = ElementId.New();
        var source = new CadReference(DrawingId.New(), new CadHandle("10"));
        var otherSource = new CadReference(DrawingId.New(), new CadHandle("20"));

        ExpectRejected(
            new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d), source),
            new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 2d), otherSource),
            "different CAD references for the same element");
        ExpectRejected(
            new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d), source),
            new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 2d)),
            "null/non-null CAD provenance for the same element");

        var matching = QuantityAccumulator.Summarize(new[]
        {
            new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d), source),
            new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 2d), source)
        });
        AssertSummary(matching, 3d, 2, 1, "matching same-element provenance");

        var nullMatching = QuantityAccumulator.Summarize(new[]
        {
            new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d)),
            new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 2d))
        });
        AssertSummary(nullMatching, 3d, 2, 1, "matching null provenance");

        var differentElements = QuantityAccumulator.Summarize(new[]
        {
            new QuantityFact(ElementId.New(), "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d), source),
            new QuantityFact(ElementId.New(), "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 2d), otherSource)
        });
        AssertSummary(differentElements, 3d, 2, 2, "independent element provenance");

        Console.WriteLine("PASS quantity accumulator CAD provenance consistency enforced");
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

    private static void AssertSummary(IReadOnlyList<QuantitySummary> summaries, double value, int factCount, int elementCount, string scenario)
    {
        if (summaries.Count != 1
            || summaries[0].Quantity.Value != value
            || summaries[0].FactCount != factCount
            || summaries[0].ElementCount != elementCount)
            throw new InvalidOperationException($"Quantity accumulator corrupted {scenario}.");
    }
}

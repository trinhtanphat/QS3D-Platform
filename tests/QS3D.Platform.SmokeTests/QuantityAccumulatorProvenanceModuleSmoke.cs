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
        var first = new QuantityFact(
            elementId,
            "WALL.LENGTH",
            new QuantityValue(QuantityDimension.Length, 1d),
            new CadReference(DrawingId.New(), new CadHandle("10")));
        var second = new QuantityFact(
            elementId,
            "WALL.LENGTH",
            new QuantityValue(QuantityDimension.Length, 2d),
            new CadReference(DrawingId.New(), new CadHandle("20")));

        try
        {
            QuantityAccumulator.Summarize(new[] { first, second });
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("PASS conflicting quantity accumulator CAD provenance rejected");
            return;
        }

        throw new InvalidOperationException("Quantity accumulator accepted conflicting CAD provenance for the same semantic element and quantity key.");
    }
}

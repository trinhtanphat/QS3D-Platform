using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityAccumulatorOrderDeterminismModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var a = new QuantityFact(new ElementId(Guid.Parse("11111111-1111-1111-1111-111111111111")), "QTY", new QuantityValue(QuantityDimension.Volume, 1e16));
        var b = new QuantityFact(new ElementId(Guid.Parse("22222222-2222-2222-2222-222222222222")), "QTY", new QuantityValue(QuantityDimension.Volume, 1e15));
        var c = new QuantityFact(new ElementId(Guid.Parse("33333333-3333-3333-3333-333333333333")), "QTY", new QuantityValue(QuantityDimension.Volume, 10d));
        var d = new QuantityFact(new ElementId(Guid.Parse("33333333-3333-3333-3333-333333333333")), "QTY", new QuantityValue(QuantityDimension.Volume, 3d));

        var first = QuantityAccumulator.Summarize(new[] { a, b, c, d }).Single();
        var permuted = QuantityAccumulator.Summarize(new[] { b, d, a, c }).Single();

        if (!first.Quantity.Value.Equals(permuted.Quantity.Value))
            throw new InvalidOperationException($"Quantity accumulation depends on enumeration order: {first.Quantity.Value:R} vs {permuted.Quantity.Value:R}.");
        if (first.FactCount != 4 || permuted.FactCount != 4)
            throw new InvalidOperationException("Permutation changed fact count.");
        if (first.ElementCount != 3 || permuted.ElementCount != 3)
            throw new InvalidOperationException("Permutation changed unique element count.");
        if (!StringComparer.Ordinal.Equals(first.Code, "QTY") || first.Quantity.Dimension != QuantityDimension.Volume)
            throw new InvalidOperationException("Permutation regression changed summary identity.");

        Console.WriteLine("PASS quantity accumulation permutation determinism");
    }
}

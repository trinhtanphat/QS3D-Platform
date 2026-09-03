using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqProjectionReadonlyLinesModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var first = new BoqLine(
            "A.LENGTH",
            new QuantityValue(QuantityDimension.Length, 2d),
            1,
            5m,
            new Money(10m, "USD"));
        var second = new BoqLine(
            "B.AREA",
            new QuantityValue(QuantityDimension.Area, 3d),
            1,
            7m,
            new Money(21m, "USD"));
        var projection = new BoqProjection(new[] { second, first }, "USD");

        if (projection.Total.Amount != 31m)
            throw new InvalidOperationException("BOQ projection baseline total changed unexpectedly.");
        if (!ReferenceEquals(projection.Lines[0], first) || !ReferenceEquals(projection.Lines[1], second))
            throw new InvalidOperationException("BOQ projection ordering or line identity changed.");

        AssertReadOnlyView(projection.Lines);

        Console.WriteLine("PASS BOQ projection lines cannot mutate after commercial total validation");
    }

    private static void AssertReadOnlyView(IReadOnlyList<BoqLine> lines)
    {
        if (lines is BoqLine[])
            throw new InvalidOperationException("BoqProjection.Lines exposes its validated backing array.");

        if (lines is IList<BoqLine> mutableView)
        {
            var replacement = new BoqLine(
                "A.LENGTH",
                new QuantityValue(QuantityDimension.Length, 4d),
                1,
                5m,
                new Money(20m, "USD"));
            try
            {
                mutableView[0] = replacement;
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new InvalidOperationException("BoqProjection.Lines permits mutation after aggregate total validation.");
        }
    }
}

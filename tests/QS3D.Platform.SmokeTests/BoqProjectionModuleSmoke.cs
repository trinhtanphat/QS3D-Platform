using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqProjectionModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var quantities = new[]
        {
            new QuantitySummary("WALL.AREA", QuantityDimension.Area, 12.5, 2, 2),
            new QuantitySummary("CONCRETE.VOLUME", QuantityDimension.Volume, 3.25, 1, 1)
        };
        var rates = new[]
        {
            new UnitRate("WALL.AREA", QuantityDimension.Area, 100_000m, "vnd"),
            new UnitRate("CONCRETE.VOLUME", QuantityDimension.Volume, 1_500_000m, "VND")
        };

        var boq = BoqProjector.Project(quantities, rates, "VND");
        Require(boq.Lines.Count == 2, "two quantity summaries must produce two BQ lines");
        Require(boq.Total == new Money(6_125_000m, "VND"), "BQ total must be deterministic");
        Require(boq.Lines.Single(static line => line.Code == "WALL.AREA").Total.Amount == 1_250_000m, "wall-area cost mismatch");

        Throws<InvalidOperationException>(() => BoqProjector.Project(
            quantities,
            new[] { new UnitRate("WALL.AREA", QuantityDimension.Area, 1m, "USD") },
            "VND"));

        Throws<InvalidOperationException>(() => BoqProjector.Project(
            quantities,
            new[] { new UnitRate("WALL.AREA", QuantityDimension.Area, 1m, "VND") },
            "VND"));

        Console.WriteLine("PASS BQ cost projection module");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}

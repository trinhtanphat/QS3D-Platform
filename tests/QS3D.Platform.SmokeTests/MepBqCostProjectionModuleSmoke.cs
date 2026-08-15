using System.Runtime.CompilerServices;
using QS3D.Platform.Parity;

namespace QS3D.Platform.SmokeTests;

internal static class MepBqCostProjectionModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var groups = new MepQuantityService().Aggregate(new[]
        {
            new MepElement("P1", MepElementKind.Pipe, "CHW", "DN50", "L1", lengthM: 5d),
            new MepElement("P2", MepElementKind.Pipe, "CHW", "DN50", "L1", lengthM: 7d),
            new MepElement("D1", MepElementKind.Duct, "HVAC", "500x300", "L1", volumeM3: 2.5d),
            new MepElement("T1", MepElementKind.CableTray, "ELV", "300", "L2", areaM2: 4.25d),
            new MepElement("E1", MepElementKind.Equipment, "HVAC", "AHU-01", "L2", count: 2)
        });

        var library = new BqLibraryCatalog(new[]
        {
            new BqLibraryItem("BQ-PIPE-CHW", "CHW pipe", "m", "MEP/Pipe"),
            new BqLibraryItem("BQ-PIPE-GENERIC", "Generic pipe", "m", "MEP/Pipe"),
            new BqLibraryItem("BQ-DUCT", "Duct volume", "m3", "MEP/Duct"),
            new BqLibraryItem("BQ-TRAY", "Cable tray area", "m2", "MEP/Electrical"),
            new BqLibraryItem("BQ-EQUIP", "Equipment", "ea", "MEP/Equipment")
        });
        var profile = new MepBqMappingProfile(new[]
        {
            new MepBqMappingRule("pipe.generic", 10, "BQ-PIPE-GENERIC", MepBqMeasurementBasis.Length, MepElementKind.Pipe),
            new MepBqMappingRule("pipe.chw", 20, "BQ-PIPE-CHW", MepBqMeasurementBasis.Length, MepElementKind.Pipe, system: "CHW"),
            new MepBqMappingRule("duct.volume", 10, "BQ-DUCT", MepBqMeasurementBasis.Volume, MepElementKind.Duct),
            new MepBqMappingRule("tray.area", 10, "BQ-TRAY", MepBqMeasurementBasis.Area, MepElementKind.CableTray),
            new MepBqMappingRule("equipment.count", 10, "BQ-EQUIP", MepBqMeasurementBasis.Count, MepElementKind.Equipment)
        });

        var projected = new MepBqProjectionService().Project(groups, profile, library);
        Equal(4, projected.Count);
        Equal(12m, projected.Single(x => x.ItemCode == "BQ-PIPE-CHW").Quantity);
        Equal(2.5m, projected.Single(x => x.ItemCode == "BQ-DUCT").Quantity);
        Equal(4.25m, projected.Single(x => x.ItemCode == "BQ-TRAY").Quantity);
        Equal(2m, projected.Single(x => x.ItemCode == "BQ-EQUIP").Quantity);
        Require(projected.All(x => x.ItemCode != "BQ-PIPE-GENERIC"), "specific CHW mapping must outrank generic pipe mapping");
        Equal(2, projected.Single(x => x.ItemCode == "BQ-PIPE-CHW").Sources.Count);

        var pipeGroup = groups.Single(x => x.Kind == MepElementKind.Pipe);
        var ambiguousProfile = new MepBqMappingProfile(new[]
        {
            new MepBqMappingRule("pipe.a", 100, "BQ-PIPE-CHW", MepBqMeasurementBasis.Length, MepElementKind.Pipe),
            new MepBqMappingRule("pipe.b", 100, "BQ-PIPE-GENERIC", MepBqMeasurementBasis.Length, MepElementKind.Pipe)
        });
        Equal(MepBqMappingStatus.Ambiguous, ambiguousProfile.Match(pipeGroup).Status);
        Throws<InvalidOperationException>(() => new MepBqProjectionService().Project(new[] { pipeGroup }, ambiguousProfile, library));

        var wrongUnitLibrary = new BqLibraryCatalog(new[]
        {
            new BqLibraryItem("BQ-DUCT", "Duct wrong unit", "m2", "MEP/Duct")
        });
        var ductOnly = groups.Single(x => x.Kind == MepElementKind.Duct);
        var ductProfile = new MepBqMappingProfile(new[]
        {
            new MepBqMappingRule("duct", 10, "BQ-DUCT", MepBqMeasurementBasis.Volume, MepElementKind.Duct)
        });
        Throws<InvalidOperationException>(() => new MepBqProjectionService().Project(new[] { ductOnly }, ductProfile, wrongUnitLibrary));

        var rates = new[]
        {
            Rate("R-PIPE", "BQ-PIPE-CHW", "m", 10m),
            Rate("R-DUCT", "BQ-DUCT", "m3", 20m),
            Rate("R-TRAY", "BQ-TRAY", "m2", 30m),
            Rate("R-EQUIP", "BQ-EQUIP", "ea", 100m)
        };
        var cost = new MepBqCostProjectionService().Price(projected, rates, "VND");
        Equal(4, cost.Lines.Count);
        Equal(497.5m, cost.TotalCost);
        Equal(120m, cost.Lines.Single(x => x.ItemCode == "BQ-PIPE-CHW").TotalCost);

        var duplicateRates = rates.Concat(new[] { Rate("R-PIPE-ALT", "BQ-PIPE-CHW", "m", 11m) }).ToArray();
        Throws<InvalidOperationException>(() => new MepBqCostProjectionService().Price(projected, duplicateRates, "VND"));
        Throws<InvalidOperationException>(() => new MepBqCostProjectionService().Price(projected, rates, "USD"));

        Console.WriteLine("PASS shared MEP-to-BQ and cost projection");
    }

    private static CostRateBuildUp Rate(string id, string itemCode, string unit, decimal unitCost) =>
        new(id, itemCode, unit, "VND", new[]
        {
            new CostResourceComponent(id + ".RESOURCE", "Representative resource", unit, 1m, unitCost)
        });

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
    }
}

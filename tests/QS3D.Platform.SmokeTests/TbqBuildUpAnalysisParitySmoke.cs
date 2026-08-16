using QS3D.Platform.Parity;

internal static class TbqBuildUpAnalysisParitySmoke
{
    internal static void Run()
    {
        IncludesOnlyBqAdoptedRates();
        UpdatesExistingAndReturnsAffectedBq();
        ValidationFailsClosed();
    }

    private static void IncludesOnlyBqAdoptedRates()
    {
        var adopted = Rate("UR-001", 10m);
        var unused = Rate("UR-002", 20m);
        var workspace = new BuildUpAnalysisWorkspace(
            new[] { unused, adopted },
            new[]
            {
                new BqRateAdoption("BQ-002", "UR-001"),
                new BqRateAdoption("BQ-001", "ur-001")
            });

        Equal(1, workspace.Rates.Count);
        Equal("UR-001", workspace.Rates[0].Id);
        Sequence(new[] { "BQ-001", "BQ-002" }, workspace.CheckBqReversely("ur-001"));
        Throws<InvalidOperationException>(() => workspace.CheckBqReversely("UR-002"));
    }

    private static void UpdatesExistingAndReturnsAffectedBq()
    {
        var original = Rate("UR-001", 10m);
        var workspace = new BuildUpAnalysisWorkspace(
            new[] { original, Rate("UR-002", 99m) },
            new[] { new BqRateAdoption("BQ-001", "UR-001") });

        var replacement = Rate("ur-001", 25m);
        var change = workspace.UpdateExisting(replacement);

        Equal(10m, change.Previous.UnitRate);
        Equal(25m, change.Current.UnitRate);
        Sequence(new[] { "BQ-001" }, change.AffectedBqItemCodes);
        Equal(25m, change.Workspace.Rates.Single().UnitRate);
        Equal(10m, workspace.Rates.Single().UnitRate);
        Throws<InvalidOperationException>(() => workspace.UpdateExisting(Rate("UR-002", 30m)));
        Throws<InvalidOperationException>(() => workspace.UpdateExisting(Rate("UR-NEW", 30m)));
    }

    private static void ValidationFailsClosed()
    {
        Throws<ArgumentException>(() => _ = new BuildUpAnalysisWorkspace(
            new[] { Rate("UR", 1m), Rate("ur", 2m) },
            Array.Empty<BqRateAdoption>()));

        Throws<ArgumentException>(() => _ = new BuildUpAnalysisWorkspace(
            new[] { Rate("UR", 1m) },
            new[] { new BqRateAdoption("BQ", "MISSING") }));

        Throws<ArgumentException>(() => _ = new BuildUpAnalysisWorkspace(
            new[] { Rate("UR", 1m) },
            new[]
            {
                new BqRateAdoption("BQ", "UR"),
                new BqRateAdoption("bq", "ur")
            }));
    }

    private static CostRateBuildUp Rate(string id, decimal directUnitCost) =>
        new CostRateBuildUp(
            id,
            "ITEM-" + id.ToUpperInvariant(),
            "m",
            "VND",
            new[] { new CostResourceComponent("RES", "Resource", "m", 1m, directUnitCost) });

    private static void Sequence<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        var expectedArray = expected.ToArray();
        var actualArray = actual.ToArray();
        if (!expectedArray.SequenceEqual(actualArray))
            throw new InvalidOperationException("Expected [" + string.Join(",", expectedArray) + "] but got [" + string.Join(",", actualArray) + "].");
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
    }
}

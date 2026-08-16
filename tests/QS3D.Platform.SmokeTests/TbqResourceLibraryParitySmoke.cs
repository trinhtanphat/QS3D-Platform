using QS3D.Platform.Parity;

internal static class TbqResourceLibraryParitySmoke
{
    internal static void Run()
    {
        CreatesLibraryFromProjectAndImportsExplicitBatch();
        PreservesRateDetailsWithoutMutation();
        ValidationFailsClosed();
    }

    private static void CreatesLibraryFromProjectAndImportsExplicitBatch()
    {
        var rateB = Rate("UR-B", 200m);
        var rateA = Rate("UR-A", 100m);
        var rateC = Rate("UR-C", 300m);
        var library = TbqResourceLibrary.ImportFromProject(
            "RL-001",
            "PROJECT-HISTORY-01",
            new[] { rateB, rateC, rateA });

        Sequence(new[] { "UR-A", "UR-B", "UR-C" }, library.Rates.Select(static rate => rate.Id));

        var import = library.BatchImport(new[] { "ur-b", "UR-A" });
        Equal("RL-001", import.LibraryId);
        Equal("PROJECT-HISTORY-01", import.SourceProjectId);
        Sequence(new[] { "UR-A", "UR-B" }, import.SourceRateIds);
        Sequence(new[] { "UR-A", "UR-B" }, import.Rates.Select(static rate => rate.Id));
    }

    private static void PreservesRateDetailsWithoutMutation()
    {
        var original = Rate("UR-A", 100m, 10m, 5m);
        var library = TbqResourceLibrary.ImportFromProject("RL", "PROJECT", new[] { original });
        var import = library.BatchImport(new[] { "UR-A" });
        var selected = import.Rates.Single();

        Equal(115.5m, selected.UnitRate);
        Equal("VND", selected.Currency);
        Equal("m", selected.Unit);
        Equal(10m, selected.OverheadPercent);
        Equal(5m, selected.ProfitPercent);
        Equal(1, selected.Components.Count);
        Equal(100m, selected.Components[0].UnitCost);
        Equal(115.5m, original.UnitRate);
        Equal("UR-A", library.Rates.Single().Id);
    }

    private static void ValidationFailsClosed()
    {
        Throws<ArgumentException>(() => TbqResourceLibrary.ImportFromProject(
            "RL",
            "PROJECT",
            new[] { Rate("UR-A", 1m), Rate("ur-a", 2m) }));

        var library = TbqResourceLibrary.ImportFromProject(
            "RL",
            "PROJECT",
            new[] { Rate("UR-A", 1m), Rate("UR-B", 2m) });

        Throws<ArgumentException>(() => library.BatchImport(Array.Empty<string>()));
        Throws<ArgumentException>(() => library.BatchImport(new[] { "UR-A", "ur-a" }));
        Throws<InvalidOperationException>(() => library.BatchImport(new[] { "MISSING" }));
        Throws<ArgumentException>(() => library.BatchImport(new[] { " " }));
    }

    private static CostRateBuildUp Rate(
        string id,
        decimal directUnitCost,
        decimal overheadPercent = 0m,
        decimal profitPercent = 0m) =>
        new(
            id,
            "ITEM-" + id.ToUpperInvariant(),
            "m",
            "VND",
            new[] { new CostResourceComponent("RES", "Resource", "m", 1m, directUnitCost) },
            overheadPercent,
            profitPercent);

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

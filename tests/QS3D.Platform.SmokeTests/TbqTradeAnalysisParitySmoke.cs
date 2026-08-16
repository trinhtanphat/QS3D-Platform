using QS3D.Platform.Parity;

internal static class TbqTradeAnalysisParitySmoke
{
    internal static void Run()
    {
        RefreshScopesCurrentNodeAndCalculatesCfa();
        SnapshotChangesOnlyOnExplicitRefresh();
        ValidationFailsClosed();
    }

    private static void RefreshScopesCurrentNodeAndCalculatesCfa()
    {
        var workspace = new TradeAnalysisWorkspace();
        var snapshot = workspace.Refresh(
            new[]
            {
                new TradeAnalysisLine("L-1", "Structural", 100m, "Project/Bill-A"),
                new TradeAnalysisLine("L-2", null, 50m, "Project/Bill-A/Element-1"),
                new TradeAnalysisLine("L-3", "MEP", 200m, "Project/Bill-B"),
                new TradeAnalysisLine("L-4", "Civil", 999m, "Project/Bill-A2")
            },
            50m,
            "project/bill-a");

        Equal("project/bill-a", snapshot.NodePath!);
        Equal(2, snapshot.SourceLineCount);
        Equal(150m, snapshot.TotalCost);
        Equal(3m, snapshot.TotalCostPerM2!.Value);
        Equal(2, snapshot.Rows.Count);

        var structural = snapshot.Rows.Single(static row => row.TradeCode == "Structural");
        Equal(100m, structural.Cost);
        Equal(2m, structural.CostPerM2!.Value);

        var unclassified = snapshot.Rows.Single(static row => row.TradeCode == "Unclassified");
        Equal(50m, unclassified.Cost);
        Equal(1m, unclassified.CostPerM2!.Value);
    }

    private static void SnapshotChangesOnlyOnExplicitRefresh()
    {
        var workspace = new TradeAnalysisWorkspace();
        var lines = new List<TradeAnalysisLine>
        {
            new("L-1", "Structural", 100m, "Project")
        };

        var first = workspace.Refresh(lines, 0m);
        Equal(100m, first.TotalCost);
        Equal<decimal?>(null, first.TotalCostPerM2);

        lines.Add(new TradeAnalysisLine("L-2", "structural", 50m, "Project"));
        Equal(100m, workspace.Current!.TotalCost);
        Equal(1, workspace.Current.SourceLineCount);

        var second = workspace.Refresh(lines, 25m);
        Equal(150m, second.TotalCost);
        Equal(6m, second.TotalCostPerM2!.Value);
        Equal(1, second.Rows.Count);
        Equal(150m, second.Rows[0].Cost);
        Equal(6m, second.Rows[0].CostPerM2!.Value);
    }

    private static void ValidationFailsClosed()
    {
        var workspace = new TradeAnalysisWorkspace();
        Throws<ArgumentOutOfRangeException>(() => workspace.Refresh(
            new[] { new TradeAnalysisLine("L", "Trade", 1m) },
            -1m));
        Throws<ArgumentOutOfRangeException>(() => _ = new TradeAnalysisLine("L", "Trade", -1m));
        Throws<ArgumentException>(() => workspace.Refresh(
            new[]
            {
                new TradeAnalysisLine("L", "A", 1m),
                new TradeAnalysisLine("l", "B", 2m)
            },
            10m));
        Throws<OverflowException>(() => workspace.Refresh(
            new[]
            {
                new TradeAnalysisLine("L-1", "A", decimal.MaxValue),
                new TradeAnalysisLine("L-2", "A", 1m)
            },
            10m));
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

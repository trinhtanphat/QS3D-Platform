using QS3D.Platform.Parity;

internal static class TbqElementAnalysisParitySmoke
{
    internal static void Run()
    {
        AggregatesByElementAndArea();
        FiltersCurrentNodeDeterministically();
        LargeShareDoesNotOverflow();
        ValidationFailsClosed();
    }

    private static void AggregatesByElementAndArea()
    {
        var result = ElementCostAnalysisService.Analyze(new[]
        {
            new ElementCostLine("L3", "Structure", 300m, "Project/Bill-B/Element-2"),
            new ElementCostLine("L1", "Envelope", 200m, "Project/Bill-A/Element-1"),
            new ElementCostLine("L2", "envelope", 100m, "Project/Bill-A/Element-1"),
            new ElementCostLine("L4", null, 50m, "Project/Bill-A/Element-3")
        }, 100m);

        Equal(650m, result.TotalCost);
        Equal(6.5m, result.TotalCostPerM2!.Value);
        Equal(4, result.SourceLineCount);
        Equal(3, result.Rows.Count);

        var envelope = result.Rows.Single(static row => StringComparer.OrdinalIgnoreCase.Equals(row.ElementCode, "Envelope"));
        Equal(300m, envelope.Cost);
        Equal(3m, envelope.CostPerM2!.Value);
        Equal(2, envelope.SourceLineCount);
        Equal(300m * 100m / 650m, envelope.SharePercent);

        var unclassified = result.Rows.Single(static row => row.ElementCode == "Unclassified");
        Equal(50m, unclassified.Cost);
    }

    private static void FiltersCurrentNodeDeterministically()
    {
        var lines = new[]
        {
            new ElementCostLine("B", "B", 20m, "Project/Bill-B"),
            new ElementCostLine("A2", "A", 30m, "project/bill-a/element-1"),
            new ElementCostLine("A1", "a", 10m, "Project/Bill-A"),
            new ElementCostLine("C", "C", 999m, "Project/Bill-AB")
        };

        var result = ElementCostAnalysisService.Analyze(lines, 0m, "PROJECT/BILL-A");
        Equal("PROJECT/BILL-A", result.NodePath!);
        Equal(40m, result.TotalCost);
        Require(!result.TotalCostPerM2.HasValue, "zero analysis area must not invent cost/m2");
        Equal(2, result.SourceLineCount);
        Equal(1, result.Rows.Count);
        Equal("A", result.Rows[0].ElementCode);
        Equal(40m, result.Rows[0].Cost);
        Require(!result.Rows[0].CostPerM2.HasValue, "zero analysis area must not invent row cost/m2");
    }

    private static void LargeShareDoesNotOverflow()
    {
        var result = ElementCostAnalysisService.Analyze(new[]
        {
            new ElementCostLine("MAX", "Structure", decimal.MaxValue)
        }, 0m);

        Equal(decimal.MaxValue, result.TotalCost);
        Equal(1, result.Rows.Count);
        Equal(100m, result.Rows[0].SharePercent);
    }

    private static void ValidationFailsClosed()
    {
        Throws<ArgumentOutOfRangeException>(() => _ = new ElementCostLine("L", "A", -1m));
        Throws<ArgumentOutOfRangeException>(() => ElementCostAnalysisService.Analyze(Array.Empty<ElementCostLine>(), -1m));
        Throws<ArgumentException>(() => ElementCostAnalysisService.Analyze(new[]
        {
            new ElementCostLine("DUP", "A", 1m),
            new ElementCostLine("dup", "B", 2m)
        }, 1m));
        Throws<ArgumentException>(() => ElementCostAnalysisService.Analyze(new ElementCostLine[]
        {
            null!
        }, 1m));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
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

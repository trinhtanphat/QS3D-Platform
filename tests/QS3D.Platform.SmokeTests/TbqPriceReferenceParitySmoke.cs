using QS3D.Platform.Parity;

internal static class TbqPriceReferenceParitySmoke
{
    internal static void Run()
    {
        ReferenceMarksAndReverseChecks();
        IntegrityFailsClosed();
    }

    private static void ReferenceMarksAndReverseChecks()
    {
        var graph = new CostRateReferenceGraph(
            new[]
            {
                new CostRateNode("UR-001", "Concrete unit rate", CostRateKind.UnitRate),
                new CostRateNode("MAT-001", "Concrete material", CostRateKind.Material),
                new CostRateNode("LAB-001", "Concrete labor", CostRateKind.Labor),
                new CostRateNode("EQP-001", "Concrete equipment", CostRateKind.Equipment),
                new CostRateNode("MAT-UNUSED", "Unused material", CostRateKind.Material)
            },
            new[]
            {
                new CostRateCompositionLink("UR-001", "MAT-001"),
                new CostRateCompositionLink("UR-001", "LAB-001"),
                new CostRateCompositionLink("UR-001", "EQP-001")
            },
            new[]
            {
                new BqRateAdoption("BQ-002", "MAT-001"),
                new BqRateAdoption("BQ-001", "UR-001")
            });

        Equal("BQ", graph.GetReferenceState("ur-001").ReferenceMark);
        Equal("BQ+UR", graph.GetReferenceState("MAT-001").ReferenceMark);
        Equal("UR", graph.GetReferenceState("lab-001").ReferenceMark);
        Equal(string.Empty, graph.GetReferenceState("MAT-UNUSED").ReferenceMark);

        Sequence(new[] { "UR-001" }, graph.CheckLinkingRates("mat-001"));
        Sequence(new[] { "BQ-001" }, graph.CheckBqReversely("UR-001"));
        Sequence(new[] { "BQ-002" }, graph.CheckBqReversely("mat-001"));
        Sequence(Array.Empty<string>(), graph.CheckBqReversely("LAB-001"));
        Sequence(new[] { "EQP-001", "LAB-001", "MAT-UNUSED" }, graph.FindRatesNotAdoptedInBq().Select(static x => x.RateId));

        Equal(5, graph.Rates.Count);
        Equal("EQP-001", graph.Rates[0].RateId);
        Equal(3, graph.CompositionLinks.Count);
        Equal(2, graph.BqAdoptions.Count);
    }

    private static void IntegrityFailsClosed()
    {
        Throws<ArgumentException>(() => _ = new CostRateReferenceGraph(new[]
        {
            new CostRateNode("A", "A", CostRateKind.Material),
            new CostRateNode("a", "Duplicate", CostRateKind.Labor)
        }));

        Throws<ArgumentException>(() => _ = new CostRateReferenceGraph(
            new[] { new CostRateNode("UR", "Unit", CostRateKind.UnitRate) },
            new[] { new CostRateCompositionLink("UR", "MISSING") }));

        Throws<ArgumentException>(() => _ = new CostRateReferenceGraph(
            new[]
            {
                new CostRateNode("MAT", "Material", CostRateKind.Material),
                new CostRateNode("LAB", "Labor", CostRateKind.Labor)
            },
            new[] { new CostRateCompositionLink("MAT", "LAB") }));

        Throws<ArgumentException>(() => _ = new CostRateReferenceGraph(
            new[]
            {
                new CostRateNode("UR-A", "Unit A", CostRateKind.UnitRate),
                new CostRateNode("UR-B", "Unit B", CostRateKind.UnitRate)
            },
            new[] { new CostRateCompositionLink("UR-A", "UR-B") }));

        var graph = new CostRateReferenceGraph(new[]
        {
            new CostRateNode("UR", "Unit", CostRateKind.UnitRate),
            new CostRateNode("MAT", "Material", CostRateKind.Material)
        });
        Throws<ArgumentException>(() => graph.CheckLinkingRates("UR"));
        Throws<ArgumentException>(() => graph.CheckBqReversely("UNKNOWN"));
    }

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

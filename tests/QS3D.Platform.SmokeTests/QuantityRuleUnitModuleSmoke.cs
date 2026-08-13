using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Geometry;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRuleUnitModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        Equal(2.5d, QuantityUnits.ToCanonical(2500d, QuantityUnit.Millimeter));
        Equal(2.5d, QuantityUnits.ToCanonical(2_500_000d, QuantityUnit.SquareMillimeter));
        Equal(2.5d, QuantityUnits.ToCanonical(2500d, QuantityUnit.Gram));
        Equal(2500d, QuantityUnits.FromCanonical(2.5d, QuantityUnit.Millimeter));

        var tolerance = GeometryTolerance.Default;
        Require(tolerance.NearlyEqualDistance(1d, 1d + 5e-10d), "default linear tolerance must accept sub-nanometre delta");
        Require(!tolerance.NearlyEqualDistance(1d, 1.0001d), "default linear tolerance must reject material delta");
        Require(tolerance.NearlyEqualDistance(double.MaxValue, double.MaxValue * (1d - 5e-13d)), "relative tolerance must remain stable for large finite coordinates");

        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall 200");
        var project = new SemanticProject(ProjectId.New(), "Quantity Rules");
        project.AddFamily(family);
        var first = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W1", family.Id);
        first.SetProperty("LengthMm", "2500");
        first.SetProperty("HeightMm", "3000");
        project.AddElement(first);

        var catalog = new QuantityRuleCatalog(new[]
        {
            new QuantityRuleDefinition(
                SemanticElementKind.Wall,
                "WALL.LENGTH",
                QuantityDimension.Length,
                new[] { new QuantityFactor("LengthMm", QuantityUnit.Millimeter) }),
            new QuantityRuleDefinition(
                SemanticElementKind.Wall,
                "WALL.AREA",
                QuantityDimension.Area,
                new[]
                {
                    new QuantityFactor("LengthMm", QuantityUnit.Millimeter),
                    new QuantityFactor("HeightMm", QuantityUnit.Millimeter)
                }),
            new QuantityRuleDefinition(SemanticElementKind.Wall, "WALL.COUNT", QuantityDimension.Count)
        });

        var firstFacts = QuantityRuleEngine.Evaluate(project, catalog);
        Equal(3, firstFacts.Count);
        Equal(2.5d, firstFacts.Single(static fact => fact.Code == "WALL.LENGTH").Quantity.Value);
        Equal(7.5d, firstFacts.Single(static fact => fact.Code == "WALL.AREA").Quantity.Value);
        Equal(1d, firstFacts.Single(static fact => fact.Code == "WALL.COUNT").Quantity.Value);

        var second = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W2", family.Id);
        second.SetProperty("LengthMm", "1000");
        project.AddElement(second);
        Throws<InvalidOperationException>(() => QuantityRuleEngine.Evaluate(project, catalog));

        var facts = QuantityRuleEngine.Evaluate(project, catalog, skipRuleWhenInputMissing: true);
        Equal(5, facts.Count);
        var summaries = QuantityAccumulator.Summarize(facts);
        Equal(3.5d, summaries.Single(static summary => summary.Code == "WALL.LENGTH").Quantity.Value);
        Equal(2d, summaries.Single(static summary => summary.Code == "WALL.COUNT").Quantity.Value);

        var schedule = QuantityScheduleProjector.Project(project, facts);
        Equal(2, schedule.Rows.Count);
        Equal("W1", schedule.Rows[0].ElementName);
        Equal("W2", schedule.Rows[1].ElementName);

        Throws<InvalidOperationException>(() => _ = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "BAD.LENGTH",
            QuantityDimension.Length,
            new[]
            {
                new QuantityFactor("LengthMm", QuantityUnit.Millimeter),
                new QuantityFactor("HeightMm", QuantityUnit.Millimeter)
            }));

        Console.WriteLine("PASS quantity rule, unit and tolerance policy");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}

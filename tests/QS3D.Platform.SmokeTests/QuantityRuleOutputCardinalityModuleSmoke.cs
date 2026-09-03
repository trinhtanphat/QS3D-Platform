using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRuleOutputCardinalityModuleSmoke
{
    private const int MaximumFacts = 100_000;

    [ModuleInitializer]
    internal static void Run()
    {
        VerifyExactBoundaryAndSkippedRules();
        VerifyOverflowRejected();
        Console.WriteLine("PASS quantity rule output cardinality boundary");
    }

    private static void VerifyExactBoundaryAndSkippedRules()
    {
        var project = CreateTwoWallProject();
        var rules = CreateCountRules(50_000).ToList();
        rules.Add(new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "ZZ.SKIPPED.LENGTH",
            QuantityDimension.Length,
            new[] { new QuantityFactor("MissingLength", QuantityUnit.Meter) }));

        var facts = QuantityRuleEngine.Evaluate(
            project,
            new QuantityRuleCatalog(rules),
            skipRuleWhenInputMissing: true);

        Equal(MaximumFacts, facts.Count);
        if (facts.Any(static fact => fact.Code == "ZZ.SKIPPED.LENGTH"))
            throw new InvalidOperationException("Skipped missing-input rules must not consume evaluated fact cardinality.");
    }

    private static void VerifyOverflowRejected()
    {
        var project = CreateTwoWallProject();
        var catalog = new QuantityRuleCatalog(CreateCountRules(50_001));

        try
        {
            _ = QuantityRuleEngine.Evaluate(project, catalog);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("100000", StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException("QuantityRuleEngine.Evaluate must reject before producing fact 100001.");
    }

    private static SemanticProject CreateTwoWallProject()
    {
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall Family");
        var project = new SemanticProject(ProjectId.New(), "Quantity cardinality");
        project.AddFamily(family);
        project.AddElement(new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W1", family.Id));
        project.AddElement(new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W2", family.Id));
        return project;
    }

    private static IEnumerable<QuantityRuleDefinition> CreateCountRules(int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return new QuantityRuleDefinition(
                SemanticElementKind.Wall,
                "COUNT." + i.ToString("D5", System.Globalization.CultureInfo.InvariantCulture),
                QuantityDimension.Count);
        }
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }
}

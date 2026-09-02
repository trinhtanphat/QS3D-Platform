using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRuleFactorOrderDeterminismModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var familyId = new FamilyId(Guid.Parse("11111111-2222-3333-4444-555555555555"));
        var element = new SemanticElement(
            new ElementId(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            SemanticElementKind.Wall,
            "Extreme wall",
            familyId);
        element.SetProperty("HugeA", "1e308");
        element.SetProperty("HugeB", "1e308");
        element.SetProperty("Tiny", "1e-308");

        var project = new SemanticProject(
            new ProjectId(Guid.Parse("12345678-1234-1234-1234-123456789abc")),
            "Quantity rule product order smoke");
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        project.AddElement(element);

        var overflowFirst = Evaluate(project, new[]
        {
            new QuantityFactor("HugeA", QuantityUnit.Meter),
            new QuantityFactor("HugeB", QuantityUnit.Meter),
            new QuantityFactor("Tiny", QuantityUnit.Meter)
        });
        var finiteFirst = Evaluate(project, new[]
        {
            new QuantityFactor("HugeA", QuantityUnit.Meter),
            new QuantityFactor("Tiny", QuantityUnit.Meter),
            new QuantityFactor("HugeB", QuantityUnit.Meter)
        });

        if (!overflowFirst.Equals(finiteFirst))
            throw new InvalidOperationException($"Equivalent quantity factor permutations diverged: {overflowFirst:R} vs {finiteFirst:R}.");
        if (!double.IsFinite(overflowFirst) || overflowFirst <= 0d)
            throw new InvalidOperationException($"Expected a positive finite quantity result, got {overflowFirst:R}.");

        Console.WriteLine("PASS quantity rule factor-order determinism");
    }

    private static double Evaluate(SemanticProject project, IEnumerable<QuantityFactor> factors)
    {
        var rule = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "VOL",
            QuantityDimension.Volume,
            factors);
        var facts = QuantityRuleEngine.Evaluate(project, new QuantityRuleCatalog(new[] { rule }));
        return facts.Single().Quantity.Value;
    }
}

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
        element.SetProperty("TinyA", "1e-308");
        element.SetProperty("TinyB", "1e-308");
        element.SetProperty("PowerTiny", "1e-200");
        element.SetProperty("Zero", "0");
        element.SetProperty("TenA", "10");
        element.SetProperty("TenB", "10");

        var project = new SemanticProject(
            new ProjectId(Guid.Parse("12345678-1234-1234-1234-123456789abc")),
            "Quantity rule product order smoke");
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        project.AddElement(element);

        var underflowFirst = Evaluate(project, new[]
        {
            new QuantityFactor("TinyA", QuantityUnit.Meter),
            new QuantityFactor("TinyB", QuantityUnit.Meter),
            new QuantityFactor("HugeA", QuantityUnit.Meter)
        });
        var underflowAvoided = Evaluate(project, new[]
        {
            new QuantityFactor("TinyA", QuantityUnit.Meter),
            new QuantityFactor("HugeA", QuantityUnit.Meter),
            new QuantityFactor("TinyB", QuantityUnit.Meter)
        });
        if (!underflowFirst.Equals(underflowAvoided))
            throw new InvalidOperationException($"Equivalent tiny/huge factor permutations diverged: {underflowFirst:R} vs {underflowAvoided:R}.");
        if (underflowFirst <= 0d)
            throw new InvalidOperationException("Representable positive quantity was rounded to zero by intermediate underflow.");

        var overflowFirst = Evaluate(project, new[]
        {
            new QuantityFactor("HugeA", QuantityUnit.Meter),
            new QuantityFactor("HugeB", QuantityUnit.Meter),
            new QuantityFactor("TinyA", QuantityUnit.Meter)
        });
        var overflowAvoided = Evaluate(project, new[]
        {
            new QuantityFactor("HugeA", QuantityUnit.Meter),
            new QuantityFactor("TinyA", QuantityUnit.Meter),
            new QuantityFactor("HugeB", QuantityUnit.Meter)
        });
        if (!overflowFirst.Equals(overflowAvoided))
            throw new InvalidOperationException($"Equivalent huge/tiny factor permutations diverged: {overflowFirst:R} vs {overflowAvoided:R}.");
        if (!double.IsFinite(overflowFirst) || overflowFirst <= 0d)
            throw new InvalidOperationException($"Expected a positive finite quantity result, got {overflowFirst:R}.");

        var powered = Evaluate(project, new[]
        {
            new QuantityFactor("PowerTiny", QuantityUnit.Meter, exponent: 2),
            new QuantityFactor("HugeA", QuantityUnit.Meter)
        });
        if (!double.IsFinite(powered) || powered <= 0d)
            throw new InvalidOperationException($"Representable powered-factor result was lost to intermediate underflow: {powered:R}.");

        var zeroProduct = Evaluate(project, new[]
        {
            new QuantityFactor("HugeA", QuantityUnit.Meter),
            new QuantityFactor("HugeB", QuantityUnit.Meter),
            new QuantityFactor("Zero", QuantityUnit.Meter)
        });
        if (zeroProduct != 0d)
            throw new InvalidOperationException($"Zero factor must deterministically yield zero, got {zeroProduct:R}.");

        AssertGenuineOverflow(project);

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

    private static void AssertGenuineOverflow(SemanticProject project)
    {
        try
        {
            Evaluate(project, new[]
            {
                new QuantityFactor("HugeA", QuantityUnit.Meter),
                new QuantityFactor("TenA", QuantityUnit.Meter),
                new QuantityFactor("TenB", QuantityUnit.Meter)
            });
        }
        catch (OverflowException)
        {
            return;
        }

        throw new InvalidOperationException("Genuinely non-representable final quantity must fail closed with OverflowException.");
    }
}

using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRuleFinalUnderflowModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var failures = new List<string>();
        VerifyPositiveUnderflowRejected(failures);
        VerifyExplicitZeroRemainsZero(failures);
        VerifyRepresentableSubnormalRemainsPositive(failures);

        if (failures.Count != 0)
            throw new InvalidOperationException("Quantity-rule final-underflow contract failed: " + string.Join("; ", failures));

        Console.WriteLine("PASS quantity-rule final positive underflow safety");
    }

    private static void VerifyPositiveUnderflowRejected(List<string> failures)
    {
        try
        {
            _ = EvaluateArea("1e-200", "1e-200");
            failures.Add("strictly-positive 1e-400 area silently became zero");
        }
        catch (OverflowException ex) when (ex.Message.StartsWith("Quantity rule 'AREA' result underflowed", StringComparison.Ordinal))
        {
        }
        catch (Exception ex)
        {
            failures.Add("positive underflow threw unexpected " + ex.GetType().Name);
        }
    }

    private static void VerifyExplicitZeroRemainsZero(List<string> failures)
    {
        try
        {
            var fact = EvaluateArea("0", "1e-200");
            if (fact.Quantity.Value != 0d)
                failures.Add("explicit zero factor did not annihilate product");
        }
        catch (Exception ex)
        {
            failures.Add("explicit zero factor threw " + ex.GetType().Name);
        }
    }

    private static void VerifyRepresentableSubnormalRemainsPositive(List<string> failures)
    {
        try
        {
            var fact = EvaluateArea("1e-200", "1e-123");
            if (!(fact.Quantity.Value > 0d) || fact.Quantity.Value >= 2.2250738585072014e-308d)
                failures.Add("representable subnormal area was not preserved as positive subnormal");
        }
        catch (Exception ex)
        {
            failures.Add("representable subnormal area threw " + ex.GetType().Name);
        }
    }

    private static QuantityFact EvaluateArea(string a, string b)
    {
        var project = new SemanticProject(
            new ProjectId(Guid.Parse("10000000-0000-0000-0000-000000000063")),
            "Underflow project");
        var familyId = new FamilyId(Guid.Parse("20000000-0000-0000-0000-000000000063"));
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        var element = new SemanticElement(
            new ElementId(Guid.Parse("30000000-0000-0000-0000-000000000063")),
            SemanticElementKind.Wall,
            "Wall underflow probe",
            familyId);
        element.SetProperty("A", a);
        element.SetProperty("B", b);
        project.AddElement(element);

        var rule = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "AREA",
            QuantityDimension.Area,
            new[]
            {
                new QuantityFactor("A", QuantityUnit.Meter),
                new QuantityFactor("B", QuantityUnit.Meter)
            });
        var facts = QuantityRuleEngine.Evaluate(project, new QuantityRuleCatalog(new[] { rule }));
        if (facts.Count != 1)
            throw new InvalidOperationException("Underflow probe expected exactly one quantity fact.");
        return facts[0];
    }
}

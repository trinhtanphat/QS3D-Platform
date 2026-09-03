using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRuleProductRoundingModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var familyId = new FamilyId(Guid.Parse("47712c26-ec9c-44b3-a577-55b1f973747c"));
        var element = new SemanticElement(
            new ElementId(Guid.Parse("17ffc286-ef8f-4a57-a904-e16831e487fa")),
            SemanticElementKind.Wall,
            "Exact product wall",
            familyId);
        element.SetProperty("A", 1.5547907049576155d.ToString("R", CultureInfo.InvariantCulture));
        element.SetProperty("B", 1.8252483949208353d.ToString("R", CultureInfo.InvariantCulture));
        element.SetProperty("C", 1.0188123428151963d.ToString("R", CultureInfo.InvariantCulture));

        var project = new SemanticProject(
            new ProjectId(Guid.Parse("2e1f7bdd-27e8-458a-93aa-0ee00af37525")),
            "Quantity rule exact product smoke");
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        project.AddElement(element);

        var actual = Evaluate(
            project,
            new QuantityFactor("A", QuantityUnit.Each),
            new QuantityFactor("B", QuantityUnit.Each),
            new QuantityFactor("C", QuantityUnit.Each));
        const double expected = 2.891266395767554d;
        if (actual != expected)
            throw new InvalidOperationException($"Quantity rule product rounded to {actual:R}; expected exact-product rounding {expected:R}.");

        Console.WriteLine("PASS quantity rule exact-product rounding");
    }

    private static double Evaluate(SemanticProject project, params QuantityFactor[] factors)
    {
        var rule = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "COUNT.PRODUCT",
            QuantityDimension.Count,
            factors);
        var facts = QuantityRuleEngine.Evaluate(project, new QuantityRuleCatalog(new[] { rule }));
        if (facts.Count != 1 || facts[0].ElementId != project.Elements.Single().Id)
            throw new InvalidOperationException("Quantity rule product regression corrupted output fact affinity.");
        return facts[0].Quantity.Value;
    }
}

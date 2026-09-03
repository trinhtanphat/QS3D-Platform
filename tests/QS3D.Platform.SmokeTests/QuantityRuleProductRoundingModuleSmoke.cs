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
        element.SetProperty("D", 1.5d.ToString("R", CultureInfo.InvariantCulture));
        element.SetProperty("S", double.Epsilon.ToString("R", CultureInfo.InvariantCulture));

        var project = new SemanticProject(
            new ProjectId(Guid.Parse("2e1f7bdd-27e8-458a-93aa-0ee00af37525")),
            "Quantity rule exact product smoke");
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        project.AddElement(element);

        VerifyOneUlpRegressionAndPermutations(project);
        VerifyExponentSemantics(project);
        VerifySubnormalTiesToEven(project);
        VerifyFarUnderflowFailsClosed(project);

        Console.WriteLine("PASS quantity rule exact-product rounding");
    }

    private static void VerifyOneUlpRegressionAndPermutations(SemanticProject project)
    {
        const double expected = 2.891266395767554d;
        var permutations = new[]
        {
            new[] { "A", "B", "C" },
            new[] { "C", "A", "B" },
            new[] { "B", "C", "A" }
        };
        foreach (var permutation in permutations)
        {
            var factors = permutation.Select(name => new QuantityFactor(name, QuantityUnit.Each)).ToArray();
            var actual = Evaluate(project, 1d, factors);
            if (actual != expected)
                throw new InvalidOperationException($"Quantity rule product rounded to {actual:R}; expected exact-product rounding {expected:R}.");
        }
    }

    private static void VerifyExponentSemantics(SemanticProject project)
    {
        var actual = Evaluate(project, 1d, new QuantityFactor("D", QuantityUnit.Each, exponent: 3));
        if (actual != 3.375d)
            throw new InvalidOperationException($"Quantity rule exact product corrupted factor exponent semantics: {actual:R}.");
    }

    private static void VerifySubnormalTiesToEven(SemanticProject project)
    {
        var roundedUp = Evaluate(project, 1.5d, new QuantityFactor("S", QuantityUnit.Each));
        var twoEpsilon = BitConverter.Int64BitsToDouble(2L);
        if (roundedUp != twoEpsilon)
            throw new InvalidOperationException($"Quantity rule exact product failed subnormal ties-to-even rounding: {roundedUp:R}.");

        ExpectOverflow(
            "half-minimum-subnormal tie must round to zero and fail closed",
            () => Evaluate(project, 0.5d, new QuantityFactor("S", QuantityUnit.Each)));
    }

    private static void VerifyFarUnderflowFailsClosed(SemanticProject project)
    {
        ExpectOverflow(
            "far-underflow product must fail closed without constructing an oversized rounding midpoint",
            () => Evaluate(project, 1d, new QuantityFactor("S", QuantityUnit.Each, exponent: 3)));
    }

    private static double Evaluate(SemanticProject project, double multiplier, params QuantityFactor[] factors)
    {
        var rule = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "COUNT.PRODUCT",
            QuantityDimension.Count,
            factors,
            multiplier);
        var facts = QuantityRuleEngine.Evaluate(project, new QuantityRuleCatalog(new[] { rule }));
        if (facts.Count != 1 || facts[0].ElementId != project.Elements.Single().Id)
            throw new InvalidOperationException("Quantity rule product regression corrupted output fact affinity.");
        return facts[0].Quantity.Value;
    }

    private static void ExpectOverflow(string scenario, Action action)
    {
        try
        {
            action();
        }
        catch (OverflowException)
        {
            return;
        }

        throw new InvalidOperationException(scenario);
    }
}

using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRuleUnitScaleProductModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var familyId = new FamilyId(Guid.Parse("7b846b21-348c-4e8b-b893-7492f7587f44"));
        var element = new SemanticElement(
            new ElementId(Guid.Parse("8c331b35-0db8-4864-971d-7d6a75727d72")),
            SemanticElementKind.Wall,
            "Unit-scale extreme wall",
            familyId);
        element.SetProperty("ExtremeMass", 1e308d.ToString("R", CultureInfo.InvariantCulture));
        element.SetProperty("SubnormalVolume", double.Epsilon.ToString("R", CultureInfo.InvariantCulture));

        var project = new SemanticProject(
            new ProjectId(Guid.Parse("794ddbf6-5b62-4a87-b879-4569996c098a")),
            "Quantity rule unit-scale product smoke");
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        project.AddElement(element);

        var balancedMass = Evaluate(
            project,
            "MASS",
            QuantityDimension.Mass,
            1e-308d,
            new QuantityFactor("ExtremeMass", QuantityUnit.Tonne));
        AssertRelative("balanced tonne scale", balancedMass, 1000d, 1e-12d);

        var balancedVolume = Evaluate(
            project,
            "VOL",
            QuantityDimension.Volume,
            1e308d,
            new QuantityFactor("SubnormalVolume", QuantityUnit.CubicMillimeter));
        var expectedVolume = (double.Epsilon * 1e308d) * 1e-9d;
        if (!(expectedVolume > 0d) || !double.IsFinite(expectedVolume))
            throw new InvalidOperationException("Smoke fixture failed to construct a representable expected subnormal-volume product.");
        AssertRelative("balanced cubic-millimeter scale", balancedVolume, expectedVolume, 1e-12d);

        ExpectOverflow("standalone tonne conversion remains fail-closed", () =>
            QuantityUnits.ToCanonical(1e308d, QuantityUnit.Tonne));
        ExpectOverflow("standalone cubic-millimeter conversion remains fail-closed", () =>
            QuantityUnits.ToCanonical(double.Epsilon, QuantityUnit.CubicMillimeter));

        ExpectOverflow("genuine final mass overflow remains rejected", () =>
            Evaluate(
                project,
                "MASS.OVERFLOW",
                QuantityDimension.Mass,
                1d,
                new QuantityFactor("ExtremeMass", QuantityUnit.Tonne)));
        ExpectOverflow("genuine final volume underflow remains rejected", () =>
            Evaluate(
                project,
                "VOL.UNDERFLOW",
                QuantityDimension.Volume,
                1d,
                new QuantityFactor("SubnormalVolume", QuantityUnit.CubicMillimeter)));

        Console.WriteLine("PASS quantity rule unit-scale balanced product");
    }

    private static double Evaluate(
        SemanticProject project,
        string code,
        QuantityDimension dimension,
        double multiplier,
        QuantityFactor factor)
    {
        var rule = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            code,
            dimension,
            new[] { factor },
            multiplier);
        var facts = QuantityRuleEngine.Evaluate(project, new QuantityRuleCatalog(new[] { rule }));
        return facts.Single().Quantity.Value;
    }

    private static void AssertRelative(string scenario, double actual, double expected, double relativeTolerance)
    {
        if (!double.IsFinite(actual) || actual <= 0d)
            throw new InvalidOperationException($"{scenario} produced non-positive/non-finite result {actual:R}.");
        var scale = Math.Max(Math.Abs(expected), double.Epsilon);
        if (Math.Abs(actual - expected) / scale > relativeTolerance)
            throw new InvalidOperationException($"{scenario} expected {expected:R}, got {actual:R}.");
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

        throw new InvalidOperationException(scenario + " did not throw OverflowException.");
    }
}

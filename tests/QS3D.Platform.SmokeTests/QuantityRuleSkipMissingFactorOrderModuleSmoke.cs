using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRuleSkipMissingFactorOrderModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var familyId = new FamilyId(Guid.Parse("311a6d6d-62dd-4c8e-a4b2-f5a0f9f08cc4"));
        var element = new SemanticElement(
            new ElementId(Guid.Parse("71364fa0-e912-4691-bd5c-d76527a10be5")),
            SemanticElementKind.Wall,
            "Skip-missing factor-order wall",
            familyId);
        element.SetProperty("INVALID", "not-a-number");

        var project = new SemanticProject(
            new ProjectId(Guid.Parse("201faea5-1759-440b-958e-76452b8cd26e")),
            "Skip-missing factor-order smoke");
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        project.AddElement(element);

        var missingFirst = CreateCatalog(
            "COUNT.MISSING_FIRST",
            new QuantityFactor("MISSING", QuantityUnit.Each),
            new QuantityFactor("INVALID", QuantityUnit.Each));
        var invalidFirst = CreateCatalog(
            "COUNT.INVALID_FIRST",
            new QuantityFactor("INVALID", QuantityUnit.Each),
            new QuantityFactor("MISSING", QuantityUnit.Each));

        VerifySkipMissingIsFactorOrderIndependent(project, missingFirst, invalidFirst);
        VerifyNonSkipModeStillFailsClosed(project, missingFirst, invalidFirst);
        VerifyCompleteInputsRemainFactorOrderIndependent(project, element, missingFirst, invalidFirst);

        Console.WriteLine("PASS quantity rule skip-missing factor-order determinism");
    }

    private static QuantityRuleCatalog CreateCatalog(string code, params QuantityFactor[] factors)
    {
        return new QuantityRuleCatalog(new[]
        {
            new QuantityRuleDefinition(
                SemanticElementKind.Wall,
                code,
                QuantityDimension.Count,
                factors)
        });
    }

    private static void VerifySkipMissingIsFactorOrderIndependent(
        SemanticProject project,
        QuantityRuleCatalog missingFirst,
        QuantityRuleCatalog invalidFirst)
    {
        var first = QuantityRuleEngine.Evaluate(project, missingFirst, skipRuleWhenInputMissing: true);
        if (first.Count != 0)
            throw new InvalidOperationException("Missing-first rule must be skipped when a required input is absent.");

        IReadOnlyList<QuantityFact> second;
        try
        {
            second = QuantityRuleEngine.Evaluate(project, invalidFirst, skipRuleWhenInputMissing: true);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                "Skip-missing rule outcome must not depend on whether an invalid present factor appears before an absent required factor.",
                ex);
        }

        if (second.Count != 0)
            throw new InvalidOperationException("Invalid-first rule with another required input absent must also be skipped.");
    }

    private static void VerifyNonSkipModeStillFailsClosed(
        SemanticProject project,
        QuantityRuleCatalog missingFirst,
        QuantityRuleCatalog invalidFirst)
    {
        ExpectInvalidOperation(() => QuantityRuleEngine.Evaluate(project, missingFirst, skipRuleWhenInputMissing: false));
        ExpectInvalidOperation(() => QuantityRuleEngine.Evaluate(project, invalidFirst, skipRuleWhenInputMissing: false));
    }

    private static void VerifyCompleteInputsRemainFactorOrderIndependent(
        SemanticProject project,
        SemanticElement element,
        QuantityRuleCatalog missingFirst,
        QuantityRuleCatalog invalidFirst)
    {
        element.SetProperty("MISSING", "3");
        element.SetProperty("INVALID", "2");

        var first = QuantityRuleEngine.Evaluate(project, missingFirst, skipRuleWhenInputMissing: true);
        var second = QuantityRuleEngine.Evaluate(project, invalidFirst, skipRuleWhenInputMissing: true);
        if (first.Count != 1 || second.Count != 1)
            throw new InvalidOperationException("Complete-input rules must each produce one fact.");
        if (first[0].Quantity.Value != 6d || second[0].Quantity.Value != 6d)
            throw new InvalidOperationException("Complete-input commutative rules must preserve the same exact quantity across factor order.");
    }

    private static void ExpectInvalidOperation(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException("Non-skip quantity rule evaluation must remain fail-closed for incomplete or invalid input.");
    }
}

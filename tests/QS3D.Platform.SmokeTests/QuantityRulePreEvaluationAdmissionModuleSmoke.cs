using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRulePreEvaluationAdmissionModuleSmoke
{
    private const int PrefixRuleCount = 49_999;

    [ModuleInitializer]
    internal static void Run()
    {
        VerifyDoomedFactIsRejectedBeforeInputEvaluation();
        Console.WriteLine("PASS quantity rule pre-evaluation admission");
    }

    private static void VerifyDoomedFactIsRejectedBeforeInputEvaluation()
    {
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall Family");
        var project = new SemanticProject(ProjectId.New(), "Pre-evaluation admission");
        project.AddFamily(family);

        var first = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W1", family.Id);
        first.SetProperty("Length", "1");
        project.AddElement(first);

        var second = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W2", family.Id);
        second.SetProperty("Length", "not-a-number");
        project.AddElement(second);

        var rules = new List<QuantityRuleDefinition>(PrefixRuleCount + 2);
        for (var i = 0; i < PrefixRuleCount; i++)
        {
            rules.Add(new QuantityRuleDefinition(
                SemanticElementKind.Wall,
                "A.COUNT." + i.ToString("D5", CultureInfo.InvariantCulture),
                QuantityDimension.Count));
        }
        rules.Add(new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "B.LENGTH",
            QuantityDimension.Length,
            new[] { new QuantityFactor("Length", QuantityUnit.Meter) }));
        rules.Add(new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "C.COUNT",
            QuantityDimension.Count));

        try
        {
            _ = QuantityRuleEngine.Evaluate(project, new QuantityRuleCatalog(rules));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("100000", StringComparison.Ordinal))
        {
            return;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("must be a non-negative finite", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "QuantityRuleEngine evaluated malformed input for a fact that was already outside the supported output cardinality.",
                ex);
        }

        throw new InvalidOperationException("QuantityRuleEngine must reject the doomed 100001st fact before evaluating its input.");
    }
}

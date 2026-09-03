using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityCalculatedResultReadonlyModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyAccumulatorResultIsReadOnly();
        VerifyRuleEngineResultIsReadOnly();
        Console.WriteLine("PASS calculated quantity results expose immutable read-only views");
    }

    private static void VerifyAccumulatorResultIsReadOnly()
    {
        var elementId = ElementId.New();
        var firstFact = new QuantityFact(
            elementId,
            "A.LENGTH",
            new QuantityValue(QuantityDimension.Length, 2d));
        var secondFact = new QuantityFact(
            elementId,
            "B.LENGTH",
            new QuantityValue(QuantityDimension.Length, 3d));

        var summaries = QuantityAccumulator.Summarize(new[] { secondFact, firstFact });
        Equal(2, summaries.Count);
        Equal("A.LENGTH", summaries[0].Code);
        Equal("B.LENGTH", summaries[1].Code);
        AssertReadOnlyView(summaries, "QuantityAccumulator.Summarize");
    }

    private static void VerifyRuleEngineResultIsReadOnly()
    {
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall Family");
        var project = new SemanticProject(ProjectId.New(), "Readonly quantity result");
        project.AddFamily(family);

        var element = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "Wall", family.Id);
        element.SetProperty("Length", "2");
        project.AddElement(element);

        var catalog = new QuantityRuleCatalog(new[]
        {
            new QuantityRuleDefinition(
                SemanticElementKind.Wall,
                "A.COUNT",
                QuantityDimension.Count),
            new QuantityRuleDefinition(
                SemanticElementKind.Wall,
                "B.LENGTH",
                QuantityDimension.Length,
                new[] { new QuantityFactor("Length", QuantityUnit.Meter) })
        });

        var facts = QuantityRuleEngine.Evaluate(project, catalog);
        Equal(2, facts.Count);
        Equal("A.COUNT", facts[0].Code);
        Equal("B.LENGTH", facts[1].Code);
        if (facts.Any(fact => fact.ElementId != element.Id))
            throw new InvalidOperationException("QuantityRuleEngine.Evaluate changed result element identity.");
        AssertReadOnlyView(facts, "QuantityRuleEngine.Evaluate");
    }

    private static void AssertReadOnlyView<T>(IReadOnlyList<T> values, string surface)
    {
        if (values is T[])
            throw new InvalidOperationException($"{surface} exposes a mutable backing array.");
        if (values is List<T>)
            throw new InvalidOperationException($"{surface} exposes a mutable backing list.");

        if (values is IList<T> mutableView)
        {
            try
            {
                mutableView[0] = values[0];
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new InvalidOperationException($"{surface} permits mutation through IList<T>.");
        }
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }
}

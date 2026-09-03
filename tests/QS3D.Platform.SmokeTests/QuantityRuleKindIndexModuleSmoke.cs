using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRuleKindIndexModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyStableOrderedReadOnlyViews();
        VerifyEvaluationCompatibility();
        Console.WriteLine("PASS quantity rule per-kind index is stable and allocation-safe");
    }

    private static void VerifyStableOrderedReadOnlyViews()
    {
        var wallB = new QuantityRuleDefinition(SemanticElementKind.Wall, "B.COUNT", QuantityDimension.Count);
        var beam = new QuantityRuleDefinition(SemanticElementKind.Beam, "BEAM.COUNT", QuantityDimension.Count);
        var wallA = new QuantityRuleDefinition(SemanticElementKind.Wall, "A.COUNT", QuantityDimension.Count);
        var catalog = new QuantityRuleCatalog(new[] { wallB, beam, wallA });

        var firstWallView = catalog.ForKind(SemanticElementKind.Wall);
        var secondWallView = catalog.ForKind(SemanticElementKind.Wall);
        if (!ReferenceEquals(firstWallView, secondWallView))
            throw new InvalidOperationException("Repeated QuantityRuleCatalog.ForKind lookups must reuse the frozen per-kind view.");
        Equal(2, firstWallView.Count);
        if (!ReferenceEquals(firstWallView[0], wallA) || !ReferenceEquals(firstWallView[1], wallB))
            throw new InvalidOperationException("Per-kind rule ordering or identity changed.");
        AssertReadOnly(firstWallView, "wall rules");

        var firstEmptyView = catalog.ForKind(SemanticElementKind.Column);
        var secondEmptyView = catalog.ForKind(SemanticElementKind.Column);
        if (!ReferenceEquals(firstEmptyView, secondEmptyView))
            throw new InvalidOperationException("Repeated empty-kind lookups must reuse a stable empty read-only view.");
        Equal(0, firstEmptyView.Count);
    }

    private static void VerifyEvaluationCompatibility()
    {
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall family");
        var project = new SemanticProject(ProjectId.New(), "Kind-index evaluation");
        project.AddFamily(family);
        for (var i = 0; i < 512; i++)
            project.AddElement(new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W" + i, family.Id));

        var catalog = new QuantityRuleCatalog(new[]
        {
            new QuantityRuleDefinition(SemanticElementKind.Wall, "B.COUNT", QuantityDimension.Count),
            new QuantityRuleDefinition(SemanticElementKind.Wall, "A.COUNT", QuantityDimension.Count)
        });
        var facts = QuantityRuleEngine.Evaluate(project, catalog);
        Equal(1024, facts.Count);
        Equal("A.COUNT", facts[0].Code);
        Equal("B.COUNT", facts[1].Code);
    }

    private static void AssertReadOnly<T>(IReadOnlyList<T> values, string surface)
    {
        if (values is T[])
            throw new InvalidOperationException(surface + " exposes a mutable backing array.");
        if (values is IList<T> mutable)
        {
            try { mutable[0] = values[0]; }
            catch (NotSupportedException) { return; }
            throw new InvalidOperationException(surface + " permits mutation through IList<T>.");
        }
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }
}

using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRuleProjectSnapshotModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyPropertyEvaluationCannotChangeFactSourceGeneration();
        Console.WriteLine("PASS quantity rule project snapshot");
    }

    private static void VerifyPropertyEvaluationCannotChangeFactSourceGeneration()
    {
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall Family");
        var project = new SemanticProject(ProjectId.New(), "Rule snapshot");
        project.AddFamily(family);

        var element = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W1", family.Id);
        var sourceBefore = new CadReference(DrawingId.New(), new CadHandle("A1"));
        var sourceAfter = new CadReference(DrawingId.New(), new CadHandle("B2"));
        element.SetSource(sourceBefore);
        element.SetProperty("Length", "2");
        project.AddElement(element);

        ReplacePropertiesForControlledReentrantMutation(element, sourceAfter);

        var catalog = new QuantityRuleCatalog(new[]
        {
            new QuantityRuleDefinition(
                SemanticElementKind.Wall,
                "WALL.LENGTH",
                QuantityDimension.Length,
                new[] { new QuantityFactor("Length", QuantityUnit.Meter) })
        });

        var facts = QuantityRuleEngine.Evaluate(project, catalog);
        if (facts.Count != 1 || facts[0].Quantity.Value != 2d)
            throw new InvalidOperationException("Quantity rule snapshot regression produced an unexpected quantity value.");
        if (facts[0].SourceReference != sourceBefore)
            throw new InvalidOperationException(
                "QuantityRuleEngine mixed element generations: property evaluation changed live CAD provenance before fact construction.");
    }

    private static void ReplacePropertiesForControlledReentrantMutation(
        SemanticElement element,
        CadReference sourceAfter)
    {
        var field = typeof(SemanticElement).GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SemanticElement property storage field was not found.");
        var replacement = new ReentrantPropertyDictionary(element, sourceAfter)
        {
            ["Length"] = "2"
        };
        field.SetValue(element, replacement);
    }

    private sealed class ReentrantPropertyDictionary : Dictionary<string, string>, IReadOnlyDictionary<string, string>
    {
        private readonly SemanticElement _element;
        private readonly CadReference _sourceAfter;
        private bool _mutated;

        internal ReentrantPropertyDictionary(SemanticElement element, CadReference sourceAfter)
            : base(StringComparer.Ordinal)
        {
            _element = element;
            _sourceAfter = sourceAfter;
        }

        bool IReadOnlyDictionary<string, string>.TryGetValue(string key, out string value)
        {
            var found = base.TryGetValue(key, out value!);
            if (found && !_mutated)
            {
                _mutated = true;
                _element.SetSource(_sourceAfter);
            }
            return found;
        }
    }
}

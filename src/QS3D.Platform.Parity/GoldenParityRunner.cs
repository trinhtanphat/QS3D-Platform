using QS3D.Platform.Diagnostics;
using QS3D.Platform.Domain;
using QS3D.Platform.Persistence;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.Parity;

public sealed class GoldenParityResult
{
    internal GoldenParityResult(string fixtureId, SemanticProject project, ModelHealthReport health, IReadOnlyList<QuantityFact> facts, IEnumerable<string> failures)
    {
        FixtureId = fixtureId;
        Project = project;
        Health = health;
        Facts = facts;
        Failures = failures.OrderBy(static failure => failure, StringComparer.Ordinal).ToArray();
    }
    public string FixtureId { get; }
    public SemanticProject Project { get; }
    public ModelHealthReport Health { get; }
    public IReadOnlyList<QuantityFact> Facts { get; }
    public IReadOnlyList<string> Failures { get; }
    public bool Passed => Failures.Count == 0;
}

public static class GoldenParityRunner
{
    public static GoldenParityResult Run(GoldenParityFixture fixture)
    {
        if (fixture is null) throw new ArgumentNullException(nameof(fixture));
        var project = SemanticSnapshotService.Restore(fixture.Snapshot);
        var health = ModelReadinessAnalyzer.Analyze(project);
        var facts = QuantityRuleEngine.Evaluate(project, new QuantityRuleCatalog(fixture.QuantityRules), fixture.SkipRuleWhenInputMissing);
        var failures = new List<string>();
        CompareDiagnostics(fixture, health, failures);
        CompareQuantities(fixture, facts, failures);
        return new GoldenParityResult(fixture.Id, project, health, facts, failures);
    }

    private static void CompareDiagnostics(GoldenParityFixture fixture, ModelHealthReport health, ICollection<string> failures)
    {
        var actual = new HashSet<string>(health.Findings.Select(finding => GoldenParityFixture.DiagnosticKey(
            finding.Code,
            finding.Severity,
            finding.ElementId.HasValue ? finding.ElementId.Value.Value : (Guid?)null)), StringComparer.Ordinal);
        var expected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in fixture.ExpectedDiagnostics)
        {
            var key = GoldenParityFixture.DiagnosticKey(item.Code, item.Severity, item.ElementId);
            expected.Add(key);
            if (!actual.Contains(key)) failures.Add($"Missing diagnostic {key}.");
        }
        if (fixture.RejectUnexpectedDiagnostics)
        {
            foreach (var key in actual.Where(key => !expected.Contains(key))) failures.Add($"Unexpected diagnostic {key}.");
        }
    }

    private static void CompareQuantities(GoldenParityFixture fixture, IReadOnlyList<QuantityFact> facts, ICollection<string> failures)
    {
        var actual = new Dictionary<string, QuantityFact>(StringComparer.Ordinal);
        foreach (var fact in facts)
        {
            var key = GoldenParityFixture.QuantityKey(fact.ElementId.Value, fact.Code, fact.Quantity.Dimension);
            if (actual.ContainsKey(key))
            {
                failures.Add($"Duplicate quantity {key}.");
                continue;
            }
            actual.Add(key, fact);
        }

        var expected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in fixture.ExpectedQuantities)
        {
            var key = GoldenParityFixture.QuantityKey(item.ElementId, item.Code, item.Dimension);
            expected.Add(key);
            if (!actual.TryGetValue(key, out var fact))
            {
                failures.Add($"Missing quantity {key}.");
                continue;
            }
            var difference = Math.Abs(fact.Quantity.Value - item.CanonicalValue);
            if (difference > item.AbsoluteTolerance)
                failures.Add($"Quantity {key} expected {item.CanonicalValue:R} +/- {item.AbsoluteTolerance:R}, got {fact.Quantity.Value:R}.");
        }
        if (fixture.RejectUnexpectedQuantities)
        {
            foreach (var key in actual.Keys.Where(key => !expected.Contains(key))) failures.Add($"Unexpected quantity {key}.");
        }
    }
}

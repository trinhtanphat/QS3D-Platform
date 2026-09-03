using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRuleOverflowAdmissionModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var familyId = new FamilyId(Guid.Parse("77e1e6fa-7dfd-49df-9348-78f66bac37bc"));
        var element = new SemanticElement(
            new ElementId(Guid.Parse("cf8ffd2e-2780-4fc7-a223-28df51b83e48")),
            SemanticElementKind.Wall,
            "Overflow admission wall",
            familyId);
        element.SetProperty("X", 2d.ToString("R", CultureInfo.InvariantCulture));
        element.SetProperty("MAX", double.MaxValue.ToString("R", CultureInfo.InvariantCulture));

        var project = new SemanticProject(
            new ProjectId(Guid.Parse("0356c0f1-47ac-40d1-bcb6-d49ec41c8fb6")),
            "Quantity rule overflow admission smoke");
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        project.AddElement(element);

        VerifyProvableOverflowFailsBeforeOversizedRoundingAllocation(project);
        VerifyFiniteExponentBoundaryRemainsAdmitted(project);

        Console.WriteLine("PASS quantity rule final-overflow admission");
    }

    private static void VerifyProvableOverflowFailsBeforeOversizedRoundingAllocation(SemanticProject project)
    {
        const int factorCount = 4096;
        const long maximumExpectedAllocationBytes = 2_000_000;
        var factors = Enumerable.Range(0, factorCount)
            .Select(static _ => new QuantityFactor("X", QuantityUnit.Each))
            .ToArray();
        var rule = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "COUNT.OVERFLOW",
            QuantityDimension.Count,
            factors);
        var catalog = new QuantityRuleCatalog(new[] { rule });

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        try
        {
            _ = QuantityRuleEngine.Evaluate(project, catalog);
            throw new InvalidOperationException("Provably overflowing quantity-rule product must fail closed.");
        }
        catch (OverflowException)
        {
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated > maximumExpectedAllocationBytes)
            throw new InvalidOperationException(
                $"Provably overflowing quantity-rule product allocated {allocated.ToString(CultureInfo.InvariantCulture)} bytes before rejection; expected no more than {maximumExpectedAllocationBytes.ToString(CultureInfo.InvariantCulture)} bytes after overflow becomes mathematically certain.");
    }

    private static void VerifyFiniteExponentBoundaryRemainsAdmitted(SemanticProject project)
    {
        var rule = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "COUNT.MAX",
            QuantityDimension.Count,
            new[] { new QuantityFactor("MAX", QuantityUnit.Each) });
        var facts = QuantityRuleEngine.Evaluate(project, new QuantityRuleCatalog(new[] { rule }));
        if (facts.Count != 1 || facts[0].Quantity.Value != double.MaxValue)
            throw new InvalidOperationException("Finite highest-binary-exponent 1023 boundary must remain exactly representable.");
    }
}

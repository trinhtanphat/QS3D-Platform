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
        element.SetProperty("UP", 2d.ToString("R", CultureInfo.InvariantCulture));
        element.SetProperty("DOWN", 0.5d.ToString("R", CultureInfo.InvariantCulture));
        element.SetProperty("MAX", double.MaxValue.ToString("R", CultureInfo.InvariantCulture));

        var project = new SemanticProject(
            new ProjectId(Guid.Parse("0356c0f1-47ac-40d1-bcb6-d49ec41c8fb6")),
            "Quantity rule overflow admission smoke");
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        project.AddElement(element);

        VerifyCertainOverflowMatchesExistingCertainUnderflowAdmissionCost(project);
        VerifyFiniteExponentBoundaryRemainsAdmitted(project);

        Console.WriteLine("PASS quantity rule final-overflow admission");
    }

    private static void VerifyCertainOverflowMatchesExistingCertainUnderflowAdmissionCost(SemanticProject project)
    {
        const int factorCount = 4096;
        const long maximumAdmissionAllocationSkewBytes = 4096;
        var overflowCatalog = CreateCatalog("UP", "COUNT.OVERFLOW", factorCount);
        var underflowCatalog = CreateCatalog("DOWN", "COUNT.UNDERFLOW", factorCount);

        // Warm identical public evaluation paths before measurement so the comparison isolates the
        // asymmetry after exact magnitude is known rather than JIT/cold-start costs. Both products
        // use the same significand multiset; only the binary exponent differs. Certain underflow
        // already rejects immediately after highestBinaryExponent is computed, so certain overflow
        // must have the same admission shape instead of entering expensive rounding first.
        ExpectOverflow(() => QuantityRuleEngine.Evaluate(project, underflowCatalog));
        ExpectOverflow(() => QuantityRuleEngine.Evaluate(project, overflowCatalog));

        var underflowAllocated = MeasureOverflowAllocation(project, underflowCatalog);
        var overflowAllocated = MeasureOverflowAllocation(project, overflowCatalog);
        if (overflowAllocated > underflowAllocated + maximumAdmissionAllocationSkewBytes)
        {
            throw new InvalidOperationException(
                $"Provably overflowing quantity-rule product allocated {overflowAllocated.ToString(CultureInfo.InvariantCulture)} bytes versus {underflowAllocated.ToString(CultureInfo.InvariantCulture)} bytes for an equal-significand certain-underflow product; overflow must reject before exact-rational rounding allocation.");
        }
    }

    private static QuantityRuleCatalog CreateCatalog(string propertyName, string code, int factorCount)
    {
        var factors = Enumerable.Range(0, factorCount)
            .Select(_ => new QuantityFactor(propertyName, QuantityUnit.Each))
            .ToArray();
        var rule = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            code,
            QuantityDimension.Count,
            factors);
        return new QuantityRuleCatalog(new[] { rule });
    }

    private static long MeasureOverflowAllocation(SemanticProject project, QuantityRuleCatalog catalog)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        ExpectOverflow(() => QuantityRuleEngine.Evaluate(project, catalog));
        return GC.GetAllocatedBytesForCurrentThread() - before;
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

    private static void ExpectOverflow(Action action)
    {
        try
        {
            action();
        }
        catch (OverflowException)
        {
            return;
        }

        throw new InvalidOperationException("Expected quantity-rule numeric overflow/underflow rejection.");
    }
}

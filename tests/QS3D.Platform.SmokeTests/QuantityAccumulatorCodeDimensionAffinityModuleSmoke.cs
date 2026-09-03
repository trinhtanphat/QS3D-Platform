using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityAccumulatorCodeDimensionAffinityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var failures = new List<string>();
        VerifySameCodeDifferentDimensionsRejectedAcrossElements(failures);
        VerifySameCodeDifferentDimensionsRejectedWithinElement(failures);
        VerifyAmbiguityRejectionIsOrderingIndependent(failures);
        VerifySameDimensionAggregationRemainsValid(failures);
        VerifyDistinctCodesRemainValid(failures);

        if (failures.Count != 0)
            throw new InvalidOperationException("Quantity accumulator code/dimension affinity failed: " + string.Join("; ", failures));

        Console.WriteLine("PASS quantity accumulator code dimension affinity");
    }

    private static void VerifySameCodeDifferentDimensionsRejectedAcrossElements(List<string> failures)
    {
        var facts = new[]
        {
            Fact("11111111-1111-1111-1111-111111111111", "WALL.QTY", QuantityDimension.Length, 3d),
            Fact("22222222-2222-2222-2222-222222222222", "WALL.QTY", QuantityDimension.Area, 9d)
        };

        ExpectAmbiguityRejected(facts, "cross-element mixed dimensions", failures);
    }

    private static void VerifySameCodeDifferentDimensionsRejectedWithinElement(List<string> failures)
    {
        var elementId = "33333333-3333-3333-3333-333333333333";
        var facts = new[]
        {
            Fact(elementId, "WALL.QTY", QuantityDimension.Length, 4d),
            Fact(elementId, "WALL.QTY", QuantityDimension.Area, 16d)
        };

        ExpectAmbiguityRejected(facts, "same-element mixed dimensions", failures);
    }

    private static void VerifyAmbiguityRejectionIsOrderingIndependent(List<string> failures)
    {
        var facts = new[]
        {
            Fact("44444444-4444-4444-4444-444444444444", "WALL.QTY", QuantityDimension.Area, 25d),
            Fact("55555555-5555-5555-5555-555555555555", "WALL.QTY", QuantityDimension.Length, 5d)
        };

        ExpectAmbiguityRejected(facts, "reverse-dimension ordering", failures);
    }

    private static void VerifySameDimensionAggregationRemainsValid(List<string> failures)
    {
        var summaries = QuantityAccumulator.Summarize(new[]
        {
            Fact("66666666-6666-6666-6666-666666666666", "WALL.LENGTH", QuantityDimension.Length, 1.25d),
            Fact("77777777-7777-7777-7777-777777777777", "WALL.LENGTH", QuantityDimension.Length, 2.75d)
        });

        if (summaries.Count != 1
            || summaries[0].Code != "WALL.LENGTH"
            || summaries[0].Quantity.Dimension != QuantityDimension.Length
            || summaries[0].Quantity.Value != 4d
            || summaries[0].FactCount != 2
            || summaries[0].ElementCount != 2)
            failures.Add("valid same-code/same-dimension aggregation changed");
    }

    private static void VerifyDistinctCodesRemainValid(List<string> failures)
    {
        var summaries = QuantityAccumulator.Summarize(new[]
        {
            Fact("88888888-8888-8888-8888-888888888888", "WALL.LENGTH", QuantityDimension.Length, 3d),
            Fact("99999999-9999-9999-9999-999999999999", "WALL.AREA", QuantityDimension.Area, 9d)
        });

        if (summaries.Count != 2
            || summaries[0].Code != "WALL.AREA"
            || summaries[1].Code != "WALL.LENGTH")
            failures.Add("valid distinct-code facts lost deterministic code ordering");
    }

    private static void ExpectAmbiguityRejected(IEnumerable<QuantityFact> facts, string scenario, List<string> failures)
    {
        try
        {
            _ = QuantityAccumulator.Summarize(facts);
            failures.Add(scenario + " was accepted");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Quantity code 'WALL.QTY'", StringComparison.Ordinal))
        {
        }
        catch (Exception ex)
        {
            failures.Add(scenario + " threw unexpected " + ex.GetType().Name);
        }
    }

    private static QuantityFact Fact(string elementId, string code, QuantityDimension dimension, double value) =>
        new(new ElementId(Guid.Parse(elementId)), code, new QuantityValue(dimension, value));
}

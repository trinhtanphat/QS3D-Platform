using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class AggregateDuplicateIdentityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var failures = new List<string>();
        VerifyDuplicateQuantitySummaryRejected(failures);
        VerifyDuplicateScheduleElementRejected(failures);
        VerifyDuplicateBoqLineRejected(failures);
        VerifyNullBoqLineRejectedExplicitly(failures);

        if (failures.Count != 0)
            throw new InvalidOperationException("Aggregate identity boundary accepted invalid input: " + string.Join("; ", failures));

        Console.WriteLine("PASS aggregate duplicate identity safety");
    }

    private static void VerifyDuplicateQuantitySummaryRejected(List<string> failures)
    {
        var elementId = new ElementId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var familyId = new FamilyId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var quantities = new[]
        {
            new QuantitySummary("VOL", QuantityDimension.Volume, 1d, 1, 1),
            new QuantitySummary("VOL", QuantityDimension.Volume, 2d, 1, 1)
        };

        ExpectInvalidOperation(
            () => _ = new QuantityScheduleRow(elementId, "Wall 1", SemanticElementKind.Wall, familyId, "Wall family", null, null, quantities),
            "Duplicate quantity summary",
            "schedule row duplicate (Code, Dimension)",
            failures);
    }

    private static void VerifyDuplicateScheduleElementRejected(List<string> failures)
    {
        var elementId = new ElementId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var familyId = new FamilyId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var first = new QuantityScheduleRow(
            elementId,
            "Wall A",
            SemanticElementKind.Wall,
            familyId,
            "Wall family",
            null,
            null,
            new[] { new QuantitySummary("AREA", QuantityDimension.Area, 1d, 1, 1) });
        var second = new QuantityScheduleRow(
            elementId,
            "Wall B",
            SemanticElementKind.Wall,
            familyId,
            "Wall family",
            null,
            null,
            new[] { new QuantitySummary("AREA", QuantityDimension.Area, 2d, 1, 1) });

        ExpectInvalidOperation(
            () => _ = new QuantitySchedule(new[] { first, second }),
            "Duplicate schedule element",
            "schedule duplicate ElementId",
            failures);
    }

    private static void VerifyDuplicateBoqLineRejected(List<string> failures)
    {
        var quantity = new QuantityValue(QuantityDimension.Volume, 1d);
        var lines = new[]
        {
            new BoqLine("VOL", quantity, 1, 10m, new Money(10m, "USD")),
            new BoqLine("VOL", quantity, 1, 20m, new Money(20m, "USD"))
        };

        ExpectInvalidOperation(
            () => _ = new BoqProjection(lines, "USD"),
            "Duplicate BQ line",
            "BOQ duplicate (Code, Dimension)",
            failures);
    }

    private static void VerifyNullBoqLineRejectedExplicitly(List<string> failures)
    {
        try
        {
            _ = new BoqProjection(new BoqLine[] { null! }, "USD");
            failures.Add("BOQ null line");
        }
        catch (ArgumentException ex) when (ex.Message.StartsWith("BQ lines must not contain null entries", StringComparison.Ordinal))
        {
        }
        catch (Exception ex)
        {
            failures.Add("BOQ null line threw " + ex.GetType().Name + " instead of ArgumentException");
        }
    }

    private static void ExpectInvalidOperation(
        Action action,
        string expectedMessagePrefix,
        string failureName,
        List<string> failures)
    {
        try
        {
            action();
            failures.Add(failureName);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith(expectedMessagePrefix, StringComparison.Ordinal))
        {
        }
        catch (Exception ex)
        {
            failures.Add(failureName + " threw unexpected " + ex.GetType().Name);
        }
    }
}

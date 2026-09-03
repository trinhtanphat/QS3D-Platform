using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleCrossRowDimensionAffinityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var failures = new List<string>();
        VerifyPublicScheduleRejectsCrossRowDimensionAmbiguity(failures);
        VerifyReverseOrderingAlsoRejects(failures);
        VerifyProjectorRejectsCrossElementDimensionAmbiguity(failures);
        VerifySameCodeSameDimensionAcrossRowsRemainsValid(failures);
        VerifyDistinctCodesAcrossRowsRemainValid(failures);
        VerifyEmptyRowsRemainValid(failures);

        if (failures.Count != 0)
            throw new InvalidOperationException("Quantity schedule cross-row dimension affinity failed: " + string.Join("; ", failures));

        Console.WriteLine("PASS quantity schedule cross-row dimension affinity");
    }

    private static void VerifyPublicScheduleRejectsCrossRowDimensionAmbiguity(List<string> failures)
    {
        var rows = CreateAmbiguousRows();
        VerifyRejected(rows, "public schedule accepted one code with different dimensions across rows", failures);
    }

    private static void VerifyReverseOrderingAlsoRejects(List<string> failures)
    {
        var rows = CreateAmbiguousRows();
        Array.Reverse(rows);
        VerifyRejected(rows, "reverse-ordered schedule accepted one code with different dimensions across rows", failures);
    }

    private static void VerifyRejected(
        IEnumerable<QuantityScheduleRow> rows,
        string acceptanceFailure,
        List<string> failures)
    {
        try
        {
            _ = new QuantitySchedule(rows);
            failures.Add(acceptanceFailure);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Quantity code 'WALL.QTY'", StringComparison.Ordinal))
        {
        }
        catch (Exception ex)
        {
            failures.Add("cross-row rejection threw unexpected " + ex.GetType().Name);
        }
    }

    private static QuantityScheduleRow[] CreateAmbiguousRows() =>
        new[]
        {
            CreateRow("11111111-1111-1111-1111-111111111111", "Wall A", "WALL.QTY", QuantityDimension.Length, 3d),
            CreateRow("22222222-2222-2222-2222-222222222222", "Wall B", "WALL.QTY", QuantityDimension.Area, 9d)
        };

    private static void VerifyProjectorRejectsCrossElementDimensionAmbiguity(List<string> failures)
    {
        var familyId = new FamilyId(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        var firstId = new ElementId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var secondId = new ElementId(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var project = new SemanticProject(new ProjectId(Guid.Parse("99999999-9999-9999-9999-999999999999")), "P");
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        project.AddElement(new SemanticElement(firstId, SemanticElementKind.Wall, "Wall C", familyId));
        project.AddElement(new SemanticElement(secondId, SemanticElementKind.Wall, "Wall D", familyId));

        var facts = new[]
        {
            new QuantityFact(firstId, "WALL.QTY", new QuantityValue(QuantityDimension.Length, 2d)),
            new QuantityFact(secondId, "WALL.QTY", new QuantityValue(QuantityDimension.Area, 4d))
        };

        try
        {
            _ = QuantityScheduleProjector.Project(project, facts);
            failures.Add("schedule projector accepted one code with different dimensions across elements");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Quantity code 'WALL.QTY'", StringComparison.Ordinal))
        {
        }
        catch (Exception ex)
        {
            failures.Add("projector cross-element rejection threw unexpected " + ex.GetType().Name);
        }
    }

    private static void VerifySameCodeSameDimensionAcrossRowsRemainsValid(List<string> failures)
    {
        var schedule = new QuantitySchedule(new[]
        {
            CreateRow("55555555-5555-5555-5555-555555555555", "Wall E", "WALL.LENGTH", QuantityDimension.Length, 3d),
            CreateRow("66666666-6666-6666-6666-666666666666", "Wall F", "WALL.LENGTH", QuantityDimension.Length, 5d)
        });

        if (schedule.Rows.Count != 2)
            failures.Add("same-code/same-dimension rows were not preserved");
    }

    private static void VerifyDistinctCodesAcrossRowsRemainValid(List<string> failures)
    {
        var schedule = new QuantitySchedule(new[]
        {
            CreateRow("77777777-7777-7777-7777-777777777777", "Wall G", "WALL.LENGTH", QuantityDimension.Length, 3d),
            CreateRow("88888888-8888-8888-8888-888888888888", "Wall H", "WALL.AREA", QuantityDimension.Area, 9d)
        });

        if (schedule.Rows.Count != 2)
            failures.Add("valid distinct-code rows were not preserved");
    }

    private static void VerifyEmptyRowsRemainValid(List<string> failures)
    {
        var emptyRow = new QuantityScheduleRow(
            new ElementId(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb")),
            "Empty wall",
            SemanticElementKind.Wall,
            new FamilyId(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            "Wall family",
            null,
            null,
            Array.Empty<QuantitySummary>());

        var schedule = new QuantitySchedule(new[] { emptyRow });
        if (schedule.Rows.Count != 1 || schedule.Rows[0].Quantities.Count != 0)
            failures.Add("empty schedule row compatibility was lost");
    }

    private static QuantityScheduleRow CreateRow(
        string elementId,
        string elementName,
        string code,
        QuantityDimension dimension,
        double value) =>
        new(
            new ElementId(Guid.Parse(elementId)),
            elementName,
            SemanticElementKind.Wall,
            new FamilyId(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            "Wall family",
            null,
            null,
            new[] { new QuantitySummary(code, dimension, value, 1, 1) });
}

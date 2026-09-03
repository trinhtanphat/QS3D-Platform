using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleProjectSnapshotModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var project = new SemanticProject(
            new ProjectId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            "Snapshot project");
        var familyId = new FamilyId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));

        var elementA = new SemanticElement(
            new ElementId(Guid.Parse("33333333-3333-3333-3333-333333333333")),
            SemanticElementKind.Wall,
            "Wall A",
            familyId);
        var sourceAtEntry = new CadReference(
            new DrawingId(Guid.Parse("44444444-4444-4444-4444-444444444444")),
            new CadHandle("A1"));
        var sourceAfterEnumerationStarts = new CadReference(
            new DrawingId(Guid.Parse("55555555-5555-5555-5555-555555555555")),
            new CadHandle("B2"));
        elementA.SetSource(sourceAtEntry);
        project.AddElement(elementA);

        var injectedElement = new SemanticElement(
            new ElementId(Guid.Parse("66666666-6666-6666-6666-666666666666")),
            SemanticElementKind.Wall,
            "Injected wall",
            familyId);
        var fact = new QuantityFact(
            elementA.Id,
            "WALL.AREA",
            new QuantityValue(QuantityDimension.Area, 12.5d),
            sourceAtEntry);

        var schedule = QuantityScheduleProjector.Project(
            project,
            MutateProjectBeforeYield(
                () =>
                {
                    elementA.SetSource(sourceAfterEnumerationStarts);
                    project.AddElement(injectedElement);
                },
                fact),
            includeElementsWithoutQuantities: true);

        if (project.Elements.Count != 2)
            throw new InvalidOperationException("Hostile fact enumerable did not mutate the project as intended.");
        if (elementA.SourceReference != sourceAfterEnumerationStarts)
            throw new InvalidOperationException("Hostile fact enumerable did not mutate source provenance as intended.");

        if (schedule.Rows.Count != 1)
            throw new InvalidOperationException($"In-flight schedule leaked post-entry project membership; expected 1 row, got {schedule.Rows.Count}.");
        var row = schedule.Rows.Single();
        if (row.ElementId != elementA.Id)
            throw new InvalidOperationException("In-flight schedule did not remain bound to the entry element snapshot.");
        if (row.SourceReference != sourceAtEntry)
            throw new InvalidOperationException("In-flight schedule reread mutable source provenance after hostile fact enumeration began.");
        if (row.Quantities.Count != 1 || row.Quantities[0].FactCount != 1 || row.Quantities[0].ElementCount != 1)
            throw new InvalidOperationException("Entry-snapshot schedule changed quantity evidence cardinality.");

        Console.WriteLine("PASS quantity schedule project snapshot invariants");
    }

    private static IEnumerable<QuantityFact> MutateProjectBeforeYield(Action mutate, QuantityFact fact)
    {
        mutate();
        yield return fact;
    }
}

using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleLocationAffinityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyValidLocationProjectsExactly();
        VerifyNullLocationRemainsValid();
        VerifyOrphanedFloorFailsClosed(includeElementsWithoutQuantities: false);
        VerifyOrphanedFloorFailsClosed(includeElementsWithoutQuantities: true);
        VerifyOrphanedZoneFailsClosed(includeElementsWithoutQuantities: false);
        VerifyOrphanedZoneFailsClosed(includeElementsWithoutQuantities: true);
        Console.WriteLine("PASS quantity schedule location affinity");
    }

    private static void VerifyValidLocationProjectsExactly()
    {
        var fixture = CreateProjectFixture();
        var schedule = QuantityScheduleProjector.Project(fixture.Project, CreateFact(fixture.Element));
        var row = schedule.Rows.Single();
        if (row.FloorId != fixture.Floor.Id || row.ZoneId != fixture.Zone.Id)
            throw new InvalidOperationException("Quantity schedule did not preserve valid current floor/zone provenance.");
    }

    private static void VerifyNullLocationRemainsValid()
    {
        var fixture = CreateProjectFixture();
        fixture.Element.AssignLocation(null, null);
        var schedule = QuantityScheduleProjector.Project(fixture.Project, CreateFact(fixture.Element));
        var row = schedule.Rows.Single();
        if (row.FloorId.HasValue || row.ZoneId.HasValue)
            throw new InvalidOperationException("Quantity schedule did not preserve null location provenance.");
    }

    private static void VerifyOrphanedFloorFailsClosed(bool includeElementsWithoutQuantities)
    {
        var fixture = CreateProjectFixture();
        fixture.Element.AssignLocation(FloorId.New(), fixture.Zone.Id);
        ExpectLocationFailure(
            () => QuantityScheduleProjector.Project(
                fixture.Project,
                includeElementsWithoutQuantities ? Array.Empty<QuantityFact>() : CreateFact(fixture.Element),
                includeElementsWithoutQuantities),
            "floor");
    }

    private static void VerifyOrphanedZoneFailsClosed(bool includeElementsWithoutQuantities)
    {
        var fixture = CreateProjectFixture();
        fixture.Element.AssignLocation(fixture.Floor.Id, ZoneId.New());
        ExpectLocationFailure(
            () => QuantityScheduleProjector.Project(
                fixture.Project,
                includeElementsWithoutQuantities ? Array.Empty<QuantityFact>() : CreateFact(fixture.Element),
                includeElementsWithoutQuantities),
            "zone");
    }

    private static void ExpectLocationFailure(Action action, string locationKind)
    {
        try
        {
            action();
            throw new InvalidOperationException($"Quantity schedule accepted an orphaned {locationKind} reference.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains($"{locationKind} outside the project", StringComparison.Ordinal))
        {
        }
    }

    private static QuantityFact[] CreateFact(SemanticElement element) =>
        new[] { new QuantityFact(element.Id, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d), element.SourceReference) };

    private static Fixture CreateProjectFixture()
    {
        var project = new SemanticProject(ProjectId.New(), "Location affinity");
        var floor = new Floor(FloorId.New(), "L01", 0d);
        var zone = new Zone(ZoneId.New(), "Zone A");
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall");
        project.AddFloor(floor);
        project.AddZone(zone);
        project.AddFamily(family);

        var element = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W01", family.Id);
        element.AssignLocation(floor.Id, zone.Id);
        project.AddElement(element);
        return new Fixture(project, floor, zone, element);
    }

    private sealed record Fixture(SemanticProject Project, Floor Floor, Zone Zone, SemanticElement Element);
}

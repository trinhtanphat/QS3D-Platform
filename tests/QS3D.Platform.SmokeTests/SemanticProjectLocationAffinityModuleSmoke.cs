using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;

namespace QS3D.Platform.SmokeTests;

internal static class SemanticProjectLocationAffinityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        RejectsMissingFloorWithoutInsertion();
        RejectsMissingZoneWithoutInsertion();
        AcceptsRegisteredAndNullLocations();
        Console.WriteLine("PASS semantic project element location affinity contracts");
    }

    private static void RejectsMissingFloorWithoutInsertion()
    {
        var project = CreateProject(out var family);
        var element = new SemanticElement(new ElementId(Guid.NewGuid()), SemanticElementKind.Wall, "Wall A", family.Id);
        element.AssignLocation(new FloorId(Guid.NewGuid()), null);

        Throws<InvalidOperationException>(() => project.AddElement(element));
        if (project.Elements.Count != 0)
            throw new InvalidOperationException("Rejected floor-affinity admission partially inserted the element.");
    }

    private static void RejectsMissingZoneWithoutInsertion()
    {
        var project = CreateProject(out var family);
        var element = new SemanticElement(new ElementId(Guid.NewGuid()), SemanticElementKind.Wall, "Wall B", family.Id);
        element.AssignLocation(null, new ZoneId(Guid.NewGuid()));

        Throws<InvalidOperationException>(() => project.AddElement(element));
        if (project.Elements.Count != 0)
            throw new InvalidOperationException("Rejected zone-affinity admission partially inserted the element.");
    }

    private static void AcceptsRegisteredAndNullLocations()
    {
        var project = CreateProject(out var family);
        var floor = new Floor(new FloorId(Guid.NewGuid()), "L01", 0d);
        var zone = new Zone(new ZoneId(Guid.NewGuid()), "Zone A");
        project.AddFloor(floor);
        project.AddZone(zone);

        var located = new SemanticElement(new ElementId(Guid.NewGuid()), SemanticElementKind.Wall, "Wall C", family.Id);
        located.AssignLocation(floor.Id, zone.Id);
        project.AddElement(located);

        var unlocated = new SemanticElement(new ElementId(Guid.NewGuid()), SemanticElementKind.Wall, "Wall D", family.Id);
        project.AddElement(unlocated);

        if (project.Elements.Count != 2)
            throw new InvalidOperationException("Valid registered/null location admission changed unexpectedly.");
    }

    private static SemanticProject CreateProject(out Family family)
    {
        var project = new SemanticProject(new ProjectId(Guid.NewGuid()), "Affinity smoke");
        family = new Family(new FamilyId(Guid.NewGuid()), SemanticElementKind.Wall, "Wall family");
        project.AddFamily(family);
        return project;
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}

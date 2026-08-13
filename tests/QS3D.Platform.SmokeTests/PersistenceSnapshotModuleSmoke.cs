using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Persistence;

namespace QS3D.Platform.SmokeTests;

internal static class PersistenceSnapshotModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var project = BuildProject();
        var snapshot = SemanticSnapshotService.Capture(project);
        Equal(SemanticSnapshotService.CurrentSchemaVersion, snapshot.SchemaVersion);
        Equal("A", snapshot.Elements.Single().SourceReference!.Handle);

        var restored = SemanticSnapshotService.Restore(snapshot);
        Equal(project.Id, restored.Id);
        Equal(project.Name, restored.Name);
        Equal(1, restored.Floors.Count);
        Equal(1, restored.Zones.Count);
        Equal(1, restored.Families.Count);
        Equal(1, restored.Elements.Count);
        var restoredElement = restored.Elements.Single();
        Equal("W1", restoredElement.Name);
        Equal("200", restoredElement.Properties["ThicknessMm"]);
        Equal("A", restoredElement.SourceReference!.Value.Handle.Value);
        Equal("B", restoredElement.GeneratedReferences.Single().Handle.Value);

        var missingFloor = new ElementSnapshot(
            snapshot.Elements[0].Id,
            snapshot.Elements[0].Kind,
            snapshot.Elements[0].Name,
            snapshot.Elements[0].FamilyId,
            Guid.NewGuid(),
            snapshot.Elements[0].ZoneId,
            snapshot.Elements[0].SourceReference,
            snapshot.Elements[0].GeneratedReferences,
            snapshot.Elements[0].Properties);
        var invalidCrossReference = new SemanticProjectSnapshot(
            1,
            snapshot.ProjectId,
            snapshot.Name,
            snapshot.Floors,
            snapshot.Zones,
            snapshot.Families,
            new[] { missingFloor });
        Throws<InvalidDataException>(() => SemanticSnapshotService.Restore(invalidCrossReference));

        var duplicateGenerated = new ElementSnapshot(
            snapshot.Elements[0].Id,
            snapshot.Elements[0].Kind,
            snapshot.Elements[0].Name,
            snapshot.Elements[0].FamilyId,
            snapshot.Elements[0].FloorId,
            snapshot.Elements[0].ZoneId,
            snapshot.Elements[0].SourceReference,
            new[]
            {
                new CadReferenceSnapshot(snapshot.Elements[0].GeneratedReferences[0].DrawingId, "B"),
                new CadReferenceSnapshot(snapshot.Elements[0].GeneratedReferences[0].DrawingId, "000b")
            },
            snapshot.Elements[0].Properties);
        var duplicateReferenceSnapshot = new SemanticProjectSnapshot(
            1,
            snapshot.ProjectId,
            snapshot.Name,
            snapshot.Floors,
            snapshot.Zones,
            snapshot.Families,
            new[] { duplicateGenerated });
        Throws<InvalidDataException>(() => SemanticSnapshotService.Restore(duplicateReferenceSnapshot));

        var migrator = new SemanticSnapshotMigrator(new ISemanticSnapshotMigration[] { new RenameMigration() });
        var migrated = migrator.Migrate(snapshot, 2);
        Equal(2, migrated.SchemaVersion);
        Equal(snapshot.ProjectId, migrated.ProjectId);
        Equal(snapshot.Name + " migrated", migrated.Name);
        Throws<InvalidOperationException>(() => new SemanticSnapshotMigrator(new ISemanticSnapshotMigration[] { new RenameMigration(), new RenameMigration() }));
        Throws<InvalidOperationException>(() => new SemanticSnapshotMigrator(new ISemanticSnapshotMigration[] { new BadIdentityMigration() }).Migrate(snapshot, 2));

        Console.WriteLine("PASS semantic persistence snapshot and migration contracts");
    }

    private static SemanticProject BuildProject()
    {
        var project = new SemanticProject(ProjectId.New(), "Persisted Project");
        var floor = new Floor(FloorId.New(), "Level 1", 0d);
        var zone = new Zone(ZoneId.New(), "North");
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall 200");
        project.AddFloor(floor);
        project.AddZone(zone);
        project.AddFamily(family);
        var element = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W1", family.Id);
        element.AssignLocation(floor.Id, zone.Id);
        var drawing = DrawingId.New();
        element.SetSource(new CadReference(drawing, new CadHandle("000a")));
        element.AddGeneratedReference(new CadReference(drawing, new CadHandle("b")));
        element.SetProperty("ThicknessMm", "200");
        project.AddElement(element);
        return project;
    }

    private static SemanticProjectSnapshot CopyWith(SemanticProjectSnapshot source, int version, Guid projectId, string name)
        => new SemanticProjectSnapshot(version, projectId, name, source.Floors, source.Zones, source.Families, source.Elements);

    private sealed class RenameMigration : ISemanticSnapshotMigration
    {
        public int FromVersion => 1;
        public int ToVersion => 2;
        public SemanticProjectSnapshot Apply(SemanticProjectSnapshot source)
            => CopyWith(source, 2, source.ProjectId, source.Name + " migrated");
    }

    private sealed class BadIdentityMigration : ISemanticSnapshotMigration
    {
        public int FromVersion => 1;
        public int ToVersion => 2;
        public SemanticProjectSnapshot Apply(SemanticProjectSnapshot source)
            => CopyWith(source, 2, Guid.NewGuid(), source.Name);
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}

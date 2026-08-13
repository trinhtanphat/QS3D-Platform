using QS3D.Platform.Domain;

namespace QS3D.Platform.Persistence;

public static class SemanticSnapshotService
{
    public const int CurrentSchemaVersion = 1;

    public static SemanticProjectSnapshot Capture(SemanticProject project)
    {
        if (project is null) throw new ArgumentNullException(nameof(project));
        return new SemanticProjectSnapshot(
            CurrentSchemaVersion,
            project.Id.Value,
            project.Name,
            project.Floors.OrderBy(static x => x.Id.Value).Select(static floor => new FloorSnapshot(floor.Id.Value, floor.Name, floor.ElevationM)),
            project.Zones.OrderBy(static x => x.Id.Value).Select(static zone => new ZoneSnapshot(zone.Id.Value, zone.Name)),
            project.Families.OrderBy(static x => x.Id.Value).Select(static family => new FamilySnapshot(family.Id.Value, family.Kind, family.Name)),
            project.Elements.OrderBy(static x => x.Id.Value).Select(ToSnapshot));
    }

    public static SemanticProject Restore(SemanticProjectSnapshot snapshot, SemanticSnapshotMigrator? migrator = null)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
        var current = snapshot;
        if (current.SchemaVersion != CurrentSchemaVersion)
        {
            if (migrator is null) throw new InvalidDataException($"Semantic snapshot schema {current.SchemaVersion} requires migration to {CurrentSchemaVersion}.");
            current = migrator.Migrate(current, CurrentSchemaVersion);
        }
        if (current.SchemaVersion != CurrentSchemaVersion)
            throw new InvalidDataException($"Semantic migration returned schema {current.SchemaVersion}, expected {CurrentSchemaVersion}.");

        ValidateCrossReferences(current);
        try
        {
            var project = new SemanticProject(new ProjectId(current.ProjectId), current.Name);
            foreach (var floor in current.Floors) project.AddFloor(new Floor(new FloorId(floor.Id), floor.Name, floor.ElevationM));
            foreach (var zone in current.Zones) project.AddZone(new Zone(new ZoneId(zone.Id), zone.Name));
            foreach (var family in current.Families) project.AddFamily(new Family(new FamilyId(family.Id), family.Kind, family.Name));
            foreach (var source in current.Elements)
            {
                var element = new SemanticElement(new ElementId(source.Id), source.Kind, source.Name, new FamilyId(source.FamilyId));
                element.AssignLocation(source.FloorId.HasValue ? new FloorId(source.FloorId.Value) : null, source.ZoneId.HasValue ? new ZoneId(source.ZoneId.Value) : null);
                if (source.SourceReference is not null) element.SetSource(ToReference(source.SourceReference));
                foreach (var generated in source.GeneratedReferences)
                {
                    if (!element.AddGeneratedReference(ToReference(generated)))
                        throw new InvalidDataException($"Element '{source.Name}' contains duplicate generated CAD references.");
                }
                foreach (var property in source.Properties) element.SetProperty(property.Key, property.Value);
                project.AddElement(element);
            }
            return project;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException or OverflowException)
        {
            throw new InvalidDataException("Semantic snapshot could not be restored safely.", ex);
        }
    }

    private static ElementSnapshot ToSnapshot(SemanticElement element)
        => new ElementSnapshot(
            element.Id.Value,
            element.Kind,
            element.Name,
            element.FamilyId.Value,
            element.FloorId?.Value,
            element.ZoneId?.Value,
            element.SourceReference.HasValue ? ToSnapshot(element.SourceReference.Value) : null,
            element.GeneratedReferences.OrderBy(static x => x.DrawingId.Value).ThenBy(static x => x.Handle.Value, StringComparer.Ordinal).Select(ToSnapshot),
            element.Properties);

    private static CadReferenceSnapshot ToSnapshot(CadReference reference)
        => new CadReferenceSnapshot(reference.DrawingId.Value, reference.Handle.Value);

    private static CadReference ToReference(CadReferenceSnapshot snapshot)
        => new CadReference(new DrawingId(snapshot.DrawingId), new CadHandle(snapshot.Handle));

    private static void ValidateCrossReferences(SemanticProjectSnapshot snapshot)
    {
        var floorIds = Unique(snapshot.Floors.Select(static x => x.Id), "floor");
        var zoneIds = Unique(snapshot.Zones.Select(static x => x.Id), "zone");
        var families = new Dictionary<Guid, FamilySnapshot>();
        foreach (var family in snapshot.Families)
        {
            if (families.ContainsKey(family.Id)) throw new InvalidDataException($"Duplicate family ID {family.Id:D}.");
            families.Add(family.Id, family);
        }
        Unique(snapshot.Elements.Select(static x => x.Id), "element");

        foreach (var element in snapshot.Elements)
        {
            if (!families.TryGetValue(element.FamilyId, out var family)) throw new InvalidDataException($"Element '{element.Name}' references missing family {element.FamilyId:D}.");
            if (family.Kind != element.Kind) throw new InvalidDataException($"Element '{element.Name}' kind {element.Kind} does not match family kind {family.Kind}.");
            if (element.FloorId.HasValue && !floorIds.Contains(element.FloorId.Value)) throw new InvalidDataException($"Element '{element.Name}' references missing floor {element.FloorId.Value:D}.");
            if (element.ZoneId.HasValue && !zoneIds.Contains(element.ZoneId.Value)) throw new InvalidDataException($"Element '{element.Name}' references missing zone {element.ZoneId.Value:D}.");
        }
    }

    private static HashSet<Guid> Unique(IEnumerable<Guid> ids, string label)
    {
        var result = new HashSet<Guid>();
        foreach (var id in ids)
        {
            if (!result.Add(id)) throw new InvalidDataException($"Duplicate {label} ID {id:D}.");
        }
        return result;
    }
}

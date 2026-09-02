using System.Collections;
using QS3D.Platform.Domain;

namespace QS3D.Platform.Persistence;

public sealed class SemanticProjectSnapshot
{
    public SemanticProjectSnapshot(int schemaVersion, Guid projectId, string name, IEnumerable<FloorSnapshot> floors, IEnumerable<ZoneSnapshot> zones, IEnumerable<FamilySnapshot> families, IEnumerable<ElementSnapshot> elements)
    {
        if (schemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        if (projectId == Guid.Empty) throw new ArgumentException("Project ID must not be empty.", nameof(projectId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Project name must not be blank.", nameof(name));
        SchemaVersion = schemaVersion;
        ProjectId = projectId;
        Name = name.Trim();
        Floors = SnapshotGuard.Copy(floors, nameof(floors));
        Zones = SnapshotGuard.Copy(zones, nameof(zones));
        Families = SnapshotGuard.Copy(families, nameof(families));
        Elements = SnapshotGuard.Copy(elements, nameof(elements));
    }

    public int SchemaVersion { get; }
    public Guid ProjectId { get; }
    public string Name { get; }
    public IReadOnlyList<FloorSnapshot> Floors { get; }
    public IReadOnlyList<ZoneSnapshot> Zones { get; }
    public IReadOnlyList<FamilySnapshot> Families { get; }
    public IReadOnlyList<ElementSnapshot> Elements { get; }
}

public sealed class FloorSnapshot
{
    public FloorSnapshot(Guid id, string name, double elevationM)
    {
        if (id == Guid.Empty) throw new ArgumentException("Floor ID must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Floor name must not be blank.", nameof(name));
        if (double.IsNaN(elevationM) || double.IsInfinity(elevationM)) throw new ArgumentOutOfRangeException(nameof(elevationM));
        Id = id;
        Name = name.Trim();
        ElevationM = elevationM;
    }
    public Guid Id { get; }
    public string Name { get; }
    public double ElevationM { get; }
}

public sealed class ZoneSnapshot
{
    public ZoneSnapshot(Guid id, string name)
    {
        if (id == Guid.Empty) throw new ArgumentException("Zone ID must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Zone name must not be blank.", nameof(name));
        Id = id;
        Name = name.Trim();
    }
    public Guid Id { get; }
    public string Name { get; }
}

public sealed class FamilySnapshot
{
    public FamilySnapshot(Guid id, SemanticElementKind kind, string name)
    {
        if (id == Guid.Empty) throw new ArgumentException("Family ID must not be empty.", nameof(id));
        if (kind == SemanticElementKind.Unknown || !Enum.IsDefined(typeof(SemanticElementKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Family name must not be blank.", nameof(name));
        Id = id;
        Kind = kind;
        Name = name.Trim();
    }
    public Guid Id { get; }
    public SemanticElementKind Kind { get; }
    public string Name { get; }
}

public sealed class CadReferenceSnapshot
{
    public CadReferenceSnapshot(Guid drawingId, string handle)
    {
        if (drawingId == Guid.Empty) throw new ArgumentException("Drawing ID must not be empty.", nameof(drawingId));
        DrawingId = drawingId;
        Handle = new CadHandle(handle).Value;
    }
    public Guid DrawingId { get; }
    public string Handle { get; }
}

public sealed class ElementSnapshot
{
    public ElementSnapshot(Guid id, SemanticElementKind kind, string name, Guid familyId, Guid? floorId, Guid? zoneId, CadReferenceSnapshot? sourceReference, IEnumerable<CadReferenceSnapshot>? generatedReferences, IReadOnlyDictionary<string, string>? properties)
    {
        if (id == Guid.Empty) throw new ArgumentException("Element ID must not be empty.", nameof(id));
        if (kind == SemanticElementKind.Unknown || !Enum.IsDefined(typeof(SemanticElementKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (familyId == Guid.Empty) throw new ArgumentException("Family ID must not be empty.", nameof(familyId));
        if (floorId == Guid.Empty) throw new ArgumentException("Floor ID must not be empty when supplied.", nameof(floorId));
        if (zoneId == Guid.Empty) throw new ArgumentException("Zone ID must not be empty when supplied.", nameof(zoneId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Element name must not be blank.", nameof(name));
        Id = id;
        Kind = kind;
        Name = name.Trim();
        FamilyId = familyId;
        FloorId = floorId;
        ZoneId = zoneId;
        SourceReference = sourceReference;
        GeneratedReferences = generatedReferences is null ? Array.Empty<CadReferenceSnapshot>() : SnapshotGuard.Copy(generatedReferences, nameof(generatedReferences));
        Properties = SnapshotGuard.CopyProperties(properties);
    }

    public Guid Id { get; }
    public SemanticElementKind Kind { get; }
    public string Name { get; }
    public Guid FamilyId { get; }
    public Guid? FloorId { get; }
    public Guid? ZoneId { get; }
    public CadReferenceSnapshot? SourceReference { get; }
    public IReadOnlyList<CadReferenceSnapshot> GeneratedReferences { get; }
    public IReadOnlyDictionary<string, string> Properties { get; }
}

internal static class SnapshotGuard
{
    internal const int MaxCollectionEntries = 100_000;

    public static T[] Copy<T>(IEnumerable<T> values, string parameterName) where T : class
    {
        if (values is null) throw new ArgumentNullException(parameterName);

        var advertisedCount = ReadAdvertisedCount(values, parameterName);
        if (advertisedCount.HasValue && advertisedCount.Value > MaxCollectionEntries)
            throw new ArgumentException($"Snapshot collection exceeds the {MaxCollectionEntries} entry limit.", parameterName);

        var result = advertisedCount.HasValue
            ? new List<T>(advertisedCount.Value)
            : new List<T>();

        foreach (var value in values)
        {
            if (result.Count >= MaxCollectionEntries)
                throw new ArgumentException($"Snapshot collection exceeds the {MaxCollectionEntries} entry limit.", parameterName);
            if (value is null)
                throw new ArgumentException("Snapshot collection must not contain null entries.", parameterName);
            result.Add(value);
        }

        if (advertisedCount.HasValue && result.Count != advertisedCount.Value)
            throw new ArgumentException("Snapshot collection Count does not match enumeration.", parameterName);

        var finalCount = ReadAdvertisedCount(values, parameterName);
        if (advertisedCount != finalCount || (finalCount.HasValue && finalCount.Value != result.Count))
            throw new ArgumentException("Snapshot collection Count changed during materialization.", parameterName);

        return result.ToArray();
    }

    public static IReadOnlyDictionary<string, string> CopyProperties(IReadOnlyDictionary<string, string>? properties)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (properties is null) return result;

        var advertisedCount = properties.Count;
        ValidateCount(advertisedCount, nameof(properties));

        foreach (var pair in properties)
        {
            if (result.Count >= MaxCollectionEntries)
                throw new ArgumentException($"Snapshot property collection exceeds the {MaxCollectionEntries} entry limit.", nameof(properties));
            if (string.IsNullOrWhiteSpace(pair.Key)) throw new ArgumentException("Snapshot property key must not be blank.", nameof(properties));
            if (pair.Value is null) throw new ArgumentException("Snapshot property value must not be null.", nameof(properties));
            var key = pair.Key.Trim();
            if (result.ContainsKey(key)) throw new ArgumentException($"Duplicate snapshot property '{key}'.", nameof(properties));
            result.Add(key, pair.Value);
        }

        if (result.Count != advertisedCount)
            throw new ArgumentException("Snapshot property Count does not match enumeration.", nameof(properties));

        var finalCount = properties.Count;
        ValidateCount(finalCount, nameof(properties));
        if (finalCount != advertisedCount || finalCount != result.Count)
            throw new ArgumentException("Snapshot property Count changed during materialization.", nameof(properties));

        return result;
    }

    private static int? ReadAdvertisedCount<T>(IEnumerable<T> values, string parameterName)
    {
        int? count = null;
        if (values is ICollection<T> collection) MergeCount(ref count, collection.Count, parameterName);
        if (values is IReadOnlyCollection<T> readOnlyCollection) MergeCount(ref count, readOnlyCollection.Count, parameterName);
        if (values is ICollection nonGenericCollection) MergeCount(ref count, nonGenericCollection.Count, parameterName);
        return count;
    }

    private static void MergeCount(ref int? observed, int candidate, string parameterName)
    {
        ValidateCount(candidate, parameterName);
        if (observed.HasValue && observed.Value != candidate)
            throw new ArgumentException("Snapshot collection exposes conflicting Count values.", parameterName);
        observed = candidate;
    }

    private static void ValidateCount(int count, string parameterName)
    {
        if (count < 0)
            throw new ArgumentException("Snapshot collection Count must not be negative.", parameterName);
        if (count > MaxCollectionEntries)
            throw new ArgumentException($"Snapshot collection exceeds the {MaxCollectionEntries} entry limit.", parameterName);
    }
}

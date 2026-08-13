using QS3D.Platform.Geometry;

namespace QS3D.Platform.Domain;

public enum SemanticElementKind
{
    Unknown = 0,
    Wall,
    Slab,
    Beam,
    Column,
    Door,
    Window,
    Opening,
    Room,
    CurtainWall,
    Foundation,
    Rebar,
    Finish
}

public sealed class Floor
{
    public Floor(FloorId id, string name, double elevationM)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("Floor ID must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Floor name must not be blank.", nameof(name));
        Id = id;
        Name = name.Trim();
        ElevationM = Numeric.RequireFinite(elevationM, nameof(elevationM));
    }
    public FloorId Id { get; }
    public string Name { get; }
    public double ElevationM { get; }
}

public sealed class Zone
{
    public Zone(ZoneId id, string name)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("Zone ID must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Zone name must not be blank.", nameof(name));
        Id = id;
        Name = name.Trim();
    }
    public ZoneId Id { get; }
    public string Name { get; }
}

public sealed class Family
{
    public Family(FamilyId id, SemanticElementKind kind, string name)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("Family ID must not be empty.", nameof(id));
        if (kind == SemanticElementKind.Unknown) throw new ArgumentOutOfRangeException(nameof(kind));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Family name must not be blank.", nameof(name));
        Id = id;
        Kind = kind;
        Name = name.Trim();
    }
    public FamilyId Id { get; }
    public SemanticElementKind Kind { get; }
    public string Name { get; }
}

public sealed class SemanticElement
{
    private readonly HashSet<CadReference> _generated = new();
    private readonly Dictionary<string, string> _properties = new(StringComparer.Ordinal);

    public SemanticElement(ElementId id, SemanticElementKind kind, string name, FamilyId familyId)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("Element ID must not be empty.", nameof(id));
        if (kind == SemanticElementKind.Unknown) throw new ArgumentOutOfRangeException(nameof(kind));
        if (familyId.Value == Guid.Empty) throw new ArgumentException("Family ID must not be empty.", nameof(familyId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Element name must not be blank.", nameof(name));
        Id = id;
        Kind = kind;
        Name = name.Trim();
        FamilyId = familyId;
    }

    public ElementId Id { get; }
    public SemanticElementKind Kind { get; }
    public string Name { get; private set; }
    public FamilyId FamilyId { get; }
    public FloorId? FloorId { get; private set; }
    public ZoneId? ZoneId { get; private set; }
    public CadReference? SourceReference { get; private set; }
    public IReadOnlyCollection<CadReference> GeneratedReferences => _generated;
    public IReadOnlyDictionary<string, string> Properties => _properties;

    public void AssignLocation(FloorId? floorId, ZoneId? zoneId)
    {
        FloorId = floorId;
        ZoneId = zoneId;
    }

    public void SetSource(CadReference? source) => SourceReference = source;
    public bool AddGeneratedReference(CadReference reference) => _generated.Add(reference);
    public bool RemoveGeneratedReference(CadReference reference) => _generated.Remove(reference);

    public void SetProperty(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Property key must not be blank.", nameof(key));
        if (value is null) throw new ArgumentNullException(nameof(value));
        _properties[key.Trim()] = value;
    }
}

public sealed class SemanticProject
{
    private readonly Dictionary<FloorId, Floor> _floors = new();
    private readonly Dictionary<ZoneId, Zone> _zones = new();
    private readonly Dictionary<FamilyId, Family> _families = new();
    private readonly Dictionary<ElementId, SemanticElement> _elements = new();

    public SemanticProject(ProjectId id, string name)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("Project ID must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Project name must not be blank.", nameof(name));
        Id = id;
        Name = name.Trim();
    }

    public ProjectId Id { get; }
    public string Name { get; private set; }
    public IReadOnlyCollection<Floor> Floors => _floors.Values;
    public IReadOnlyCollection<Zone> Zones => _zones.Values;
    public IReadOnlyCollection<Family> Families => _families.Values;
    public IReadOnlyCollection<SemanticElement> Elements => _elements.Values;

    public void AddFloor(Floor floor)
    {
        if (floor is null) throw new ArgumentNullException(nameof(floor));
        _floors.Add(floor.Id, floor);
    }

    public void AddZone(Zone zone)
    {
        if (zone is null) throw new ArgumentNullException(nameof(zone));
        _zones.Add(zone.Id, zone);
    }

    public void AddFamily(Family family)
    {
        if (family is null) throw new ArgumentNullException(nameof(family));
        _families.Add(family.Id, family);
    }

    public void AddElement(SemanticElement element)
    {
        if (element is null) throw new ArgumentNullException(nameof(element));
        if (!_families.ContainsKey(element.FamilyId))
            throw new InvalidOperationException("Element family must belong to the project before the element is added.");
        _elements.Add(element.Id, element);
    }
}

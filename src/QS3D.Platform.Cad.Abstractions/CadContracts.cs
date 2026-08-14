using QS3D.Platform.Domain;
using QS3D.Platform.Geometry;

namespace QS3D.Platform.Cad.Abstractions;

[Flags]
public enum CadCapabilities
{
    None = 0,
    TwoDimensional = 1 << 0,
    ThreeDimensional = 1 << 1,
    Blocks = 1 << 2,
    Xrefs = 1 << 3,
    Layouts = 1 << 4,
    Plot = 1 << 5,
    NativeSolids = 1 << 6,
    BooleanSolids = 1 << 7,
    ObjectSnaps = 1 << 8,
    Grips = 1 << 9,
    Layers = 1 << 10
}

public enum CadEntityKind
{
    Unknown = 0,
    Line,
    Polyline,
    Arc,
    Circle,
    Ellipse,
    Spline,
    Point,
    Hatch,
    Text,
    MText,
    BlockReference,
    Dimension,
    Leader,
    Table,
    Image,
    Solid3d,
    Mesh
}

public enum CadTransactionMode
{
    ReadOnly,
    ReadWrite
}

public static class CadBlockReferencePropertyNames
{
    public const string BlockName = "QS3D.BlockName";
    public const string InsertionX = "QS3D.InsertionX";
    public const string InsertionY = "QS3D.InsertionY";
    public const string InsertionZ = "QS3D.InsertionZ";
    public const string UniformScale = "QS3D.UniformScale";
    public const string RotationRadians = "QS3D.RotationRadians";
}

public sealed record CadLayerSnapshot(string Name, bool IsOn = true, bool IsFrozen = false, bool IsLocked = false);

public sealed record CadEntityDraft
{
    private CadEntityKind _kind;
    private IReadOnlyDictionary<string, string>? _properties;
    private string? _layerName;

    public CadEntityDraft(CadEntityKind kind, BoundingBox3 extents, IReadOnlyDictionary<string, string>? properties = null, string? layerName = null)
    {
        Kind = kind;
        Extents = extents;
        Properties = properties;
        LayerName = layerName;
    }

    public CadEntityKind Kind
    {
        get => _kind;
        init
        {
            CadContractGuard.RequireEntityKind(value, nameof(Kind));
            _kind = value;
        }
    }

    public BoundingBox3 Extents { get; init; }

    public IReadOnlyDictionary<string, string>? Properties
    {
        get => _properties;
        init
        {
            CadContractGuard.RequireProperties(value, nameof(Properties), allowNull: true);
            _properties = value;
        }
    }

    public string? LayerName
    {
        get => _layerName;
        init
        {
            if (value is not null && string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("CAD entity draft layer name must not be blank when supplied.", nameof(LayerName));
            _layerName = value;
        }
    }
}

public sealed record CadEntitySnapshot
{
    private CadHandle _handle;
    private CadEntityKind _kind;
    private IReadOnlyDictionary<string, string> _properties = new Dictionary<string, string>();
    private string _layerName = "0";

    public CadEntitySnapshot(CadHandle handle, CadEntityKind kind, BoundingBox3 extents, IReadOnlyDictionary<string, string> properties, string layerName = "0")
    {
        Handle = handle;
        Kind = kind;
        Extents = extents;
        Properties = properties;
        LayerName = layerName;
    }

    public CadHandle Handle
    {
        get => _handle;
        init
        {
            if (string.IsNullOrWhiteSpace(value.Value))
                throw new ArgumentException("CAD entity handle must not be empty.", nameof(Handle));
            _handle = value;
        }
    }

    public CadEntityKind Kind
    {
        get => _kind;
        init
        {
            CadContractGuard.RequireEntityKind(value, nameof(Kind));
            _kind = value;
        }
    }

    public BoundingBox3 Extents { get; init; }

    public IReadOnlyDictionary<string, string> Properties
    {
        get => _properties;
        init
        {
            CadContractGuard.RequireProperties(value, nameof(Properties), allowNull: false);
            _properties = value;
        }
    }

    public string LayerName
    {
        get => _layerName;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("CAD entity layer name must not be blank.", nameof(LayerName));
            _layerName = value;
        }
    }
}

public sealed record CadBlockDefinitionSnapshot(string Name, Point3 BasePoint, IReadOnlyList<CadEntityDraft> Entities);

internal static class CadContractGuard
{
    public static void RequireEntityKind(CadEntityKind kind, string parameterName)
    {
        if (kind == CadEntityKind.Unknown || !Enum.IsDefined(typeof(CadEntityKind), kind))
            throw new ArgumentOutOfRangeException(parameterName, kind, "CAD entity kind must be a defined non-Unknown value.");
    }

    public static void RequireProperties(IReadOnlyDictionary<string, string>? properties, string parameterName, bool allowNull)
    {
        if (properties is null)
        {
            if (allowNull) return;
            throw new ArgumentNullException(parameterName);
        }

        foreach (var pair in properties)
        {
            if (pair.Key is null) throw new ArgumentException("CAD entity property key must not be null.", parameterName);
            if (pair.Value is null) throw new ArgumentException($"CAD entity property '{pair.Key}' value must not be null.", parameterName);
        }
    }
}

public interface ICadHistory
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    void Undo();
    void Redo();
}

public interface ICadTransaction : IDisposable
{
    CadTransactionMode Mode { get; }
    string CurrentLayerName { get; }
    CadEntitySnapshot? Get(CadHandle handle);
    IReadOnlyList<CadEntitySnapshot> Query();
    IReadOnlyList<CadLayerSnapshot> GetLayers();
    CadLayerSnapshot? GetLayer(string name);
    IReadOnlyList<CadBlockDefinitionSnapshot> GetBlocks();
    CadBlockDefinitionSnapshot? GetBlock(string name);
    CadHandle Append(CadEntityDraft draft);
    void Update(CadEntitySnapshot entity);
    void Erase(CadHandle handle);
    void CreateLayer(string name);
    void UpdateLayer(CadLayerSnapshot layer);
    void EraseLayer(string name);
    void SetCurrentLayer(string name);
    void CreateBlock(string name, Point3 basePoint, IReadOnlyList<CadEntityDraft> entities);
    void EraseBlock(string name);
    CadHandle InsertBlock(string name, Point3 insertionPoint, double uniformScale = 1d, double rotationRadians = 0d);
    void Commit();
}

public interface ICadDatabase
{
    CadCapabilities Capabilities { get; }
    long Revision { get; }
    ICadHistory History { get; }
    ICadTransaction BeginTransaction(CadTransactionMode mode = CadTransactionMode.ReadWrite);
}

public interface ICadSelection
{
    IReadOnlyCollection<CadHandle> Current { get; }
    void Set(IEnumerable<CadHandle> handles);
    void Clear();
}

public interface ICadEditor
{
    ICadSelection Selection { get; }
    void WriteMessage(string message);
}

public interface ICadDocument
{
    DrawingId Id { get; }
    string Name { get; }
    ICadDatabase Database { get; }
    ICadEditor Editor { get; }
}

public interface IDocumentManager
{
    IReadOnlyList<ICadDocument> Documents { get; }
    ICadDocument? ActiveDocument { get; }
    ICadDocument CreateNew(string name);
    void Activate(DrawingId id);
    bool Close(DrawingId id);
}

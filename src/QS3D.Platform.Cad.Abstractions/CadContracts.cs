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

public sealed record CadLayerSnapshot(string Name, bool IsOn = true, bool IsFrozen = false, bool IsLocked = false);
public sealed record CadEntityDraft(CadEntityKind Kind, BoundingBox3 Extents, IReadOnlyDictionary<string, string>? Properties = null, string? LayerName = null);
public sealed record CadEntitySnapshot(CadHandle Handle, CadEntityKind Kind, BoundingBox3 Extents, IReadOnlyDictionary<string, string> Properties, string LayerName = "0");

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
    CadHandle Append(CadEntityDraft draft);
    void Update(CadEntitySnapshot entity);
    void Erase(CadHandle handle);
    void CreateLayer(string name);
    void UpdateLayer(CadLayerSnapshot layer);
    void EraseLayer(string name);
    void SetCurrentLayer(string name);
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

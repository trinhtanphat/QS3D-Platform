using QS3D.Platform.Domain;
using QS3D.Platform.Geometry;

namespace QS3D.Platform.Cad.Abstractions;

public enum CadViewProjection
{
    Orthographic = 0,
    Perspective
}

public sealed record CadViewState(
    Point3 Target,
    Vector3 Direction,
    Vector3 Up,
    double Width,
    double Height,
    CadViewProjection Projection = CadViewProjection.Orthographic);

public sealed record CadHitTestResult(CadHandle Handle, Point3 WorldPoint, double DistancePixels);

public interface ICadViewportService
{
    CadViewState CurrentView { get; }
    void SetView(CadViewState view);
    void ZoomExtents();
    void ZoomWindow(BoundingBox3 bounds);
    IReadOnlyList<CadHitTestResult> HitTest(Point3 worldPoint, double aperturePixels);
    void Invalidate(IEnumerable<CadHandle> handles);
    void InvalidateAll();
}

[Flags]
public enum CadSnapKind
{
    None = 0,
    Endpoint = 1 << 0,
    Midpoint = 1 << 1,
    Center = 1 << 2,
    Intersection = 1 << 3,
    Perpendicular = 1 << 4,
    Tangent = 1 << 5,
    Nearest = 1 << 6,
    Quadrant = 1 << 7,
    Extension = 1 << 8
}

public sealed record CadSnapCandidate(
    CadHandle Handle,
    CadSnapKind Kind,
    Point3 Point,
    double DistancePixels);

public interface ICadSnapService
{
    IReadOnlyList<CadSnapCandidate> Query(Point3 worldPoint, double aperturePixels, CadSnapKind enabledKinds);
}

public enum CadXrefKind
{
    Attach = 0,
    Overlay
}

public enum CadXrefStatus
{
    Loaded = 0,
    Unloaded,
    Missing,
    Unresolved,
    CircularDependency
}

public sealed record CadXrefSnapshot(
    string Name,
    string Path,
    CadXrefKind Kind,
    CadXrefStatus Status,
    DrawingId? DrawingId = null);

public interface ICadXrefService
{
    IReadOnlyList<CadXrefSnapshot> GetXrefs();
    CadXrefSnapshot Attach(string path, string name, CadXrefKind kind);
    void Reload(string name);
    void Unload(string name);
    void Detach(string name);
}

public sealed record CadLayoutSnapshot(
    string Name,
    bool IsModel,
    double PaperWidthMm,
    double PaperHeightMm,
    string? PageSetupName = null);

public interface ICadLayoutService
{
    IReadOnlyList<CadLayoutSnapshot> GetLayouts();
    string CurrentLayoutName { get; }
    void SetCurrent(string name);
    CadLayoutSnapshot Create(string name);
    void Delete(string name);
}

public enum CadPlotTargetKind
{
    Pdf = 0,
    Printer
}

public sealed record CadPlotRequest(
    string LayoutName,
    CadPlotTargetKind TargetKind,
    string Target,
    string? PageSetupName = null);

public sealed record CadPlotResult(bool Succeeded, string? OutputPath, string? Message);

public interface ICadPlotService
{
    CadPlotResult Plot(CadPlotRequest request);
}

public enum CadSelectionMode
{
    Window = 0,
    Crossing,
    Fence,
    Lasso
}

public interface ICadSpatialSelectionService
{
    IReadOnlyList<CadHandle> SelectPolygon(IReadOnlyList<Point3> points, CadSelectionMode mode);
}

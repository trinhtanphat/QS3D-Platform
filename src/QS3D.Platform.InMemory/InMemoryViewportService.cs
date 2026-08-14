using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Geometry;

namespace QS3D.Platform.InMemory;

public sealed class InMemoryViewportService : ICadViewportService
{
    private const double PixelWidth = 1000d;
    private const double PixelHeight = 1000d;
    private readonly ICadDatabase _database;
    private CadViewState _view = new(new Point3(0, 0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0), 100d, 100d);

    public InMemoryViewportService(ICadDatabase database)
        => _database = database ?? throw new ArgumentNullException(nameof(database));

    public CadViewState CurrentView => _view;

    public void SetView(CadViewState view)
    {
        if (view is null) throw new ArgumentNullException(nameof(view));
        ValidateView(view);
        _view = view;
    }

    public void ZoomExtents()
    {
        using var tx = _database.BeginTransaction(CadTransactionMode.ReadOnly);
        var entities = tx.Query();
        if (entities.Count == 0) return;
        var bounds = new BoundingBox3(
            new Point3(
                entities.Min(static entity => entity.Extents.Min.X),
                entities.Min(static entity => entity.Extents.Min.Y),
                entities.Min(static entity => entity.Extents.Min.Z)),
            new Point3(
                entities.Max(static entity => entity.Extents.Max.X),
                entities.Max(static entity => entity.Extents.Max.Y),
                entities.Max(static entity => entity.Extents.Max.Z)));
        ZoomWindow(bounds);
    }

    public void ZoomWindow(BoundingBox3 bounds)
    {
        var width = Span(bounds.Min.X, bounds.Max.X);
        var height = Span(bounds.Min.Y, bounds.Max.Y);
        width = Math.Max(width, GeometryTolerance.Default.LinearM) * 1.05d;
        height = Math.Max(height, GeometryTolerance.Default.LinearM) * 1.05d;
        if (!Numeric.IsFinite(width) || !Numeric.IsFinite(height))
            throw new InvalidOperationException("Drawing extents are too large to represent as a finite view.");
        _view = _view with
        {
            Target = new Point3(Midpoint(bounds.Min.X, bounds.Max.X), Midpoint(bounds.Min.Y, bounds.Max.Y), Midpoint(bounds.Min.Z, bounds.Max.Z)),
            Width = width,
            Height = height
        };
    }

    public IReadOnlyList<CadHitTestResult> HitTest(Point3 worldPoint, double aperturePixels)
    {
        if (!Numeric.IsFinite(aperturePixels) || aperturePixels < 0d)
            throw new ArgumentOutOfRangeException(nameof(aperturePixels));
        using var tx = _database.BeginTransaction(CadTransactionMode.ReadOnly);
        return tx.Query()
            .Select(entity =>
            {
                var candidate = ClosestPoint(entity.Extents, worldPoint);
                return new CadHitTestResult(entity.Handle, candidate, PixelDistance(worldPoint, candidate));
            })
            .Where(static result => Numeric.IsFinite(result.DistancePixels))
            .Where(result => result.DistancePixels <= aperturePixels)
            .OrderBy(static result => result.DistancePixels)
            .ThenBy(static result => result.Handle)
            .ToArray();
    }

    public void Invalidate(IEnumerable<CadHandle> handles)
    {
        if (handles is null) throw new ArgumentNullException(nameof(handles));
        _ = handles.ToArray();
    }

    public void InvalidateAll()
    {
    }

    internal double PixelDistance(Point3 first, Point3 second)
    {
        var a = Project(first);
        var b = Project(second);
        return Hypot(a.X - b.X, a.Y - b.Y);
    }

    private PixelPoint Project(Point3 point)
    {
        var forward = Normalize(_view.Direction);
        var requestedUp = Normalize(_view.Up);
        var right = Normalize(Cross(forward, requestedUp));
        var up = Normalize(Cross(right, forward));
        var delta = point - _view.Target;
        var x = Dot(delta, right) / _view.Width * PixelWidth;
        var y = Dot(delta, up) / _view.Height * PixelHeight;
        return new PixelPoint(x, y);
    }

    private static void ValidateView(CadViewState view)
    {
        if (!Numeric.IsFinite(view.Width) || view.Width <= 0d) throw new ArgumentOutOfRangeException(nameof(view), "View width must be positive and finite.");
        if (!Numeric.IsFinite(view.Height) || view.Height <= 0d) throw new ArgumentOutOfRangeException(nameof(view), "View height must be positive and finite.");
        var direction = Normalize(view.Direction);
        var up = Normalize(view.Up);
        if (Cross(direction, up).Length <= GeometryTolerance.Default.AngularRadians)
            throw new ArgumentException("View direction and up vector must not be parallel.", nameof(view));
    }

    private static Point3 ClosestPoint(BoundingBox3 bounds, Point3 point) => new(
        Math.Max(bounds.Min.X, Math.Min(bounds.Max.X, point.X)),
        Math.Max(bounds.Min.Y, Math.Min(bounds.Max.Y, point.Y)),
        Math.Max(bounds.Min.Z, Math.Min(bounds.Max.Z, point.Z)));

    private static Vector3 Normalize(Vector3 vector)
    {
        var length = vector.Length;
        if (length <= GeometryTolerance.Default.LinearM) throw new ArgumentException("Vector must be non-zero.", nameof(vector));
        return new Vector3(vector.X / length, vector.Y / length, vector.Z / length);
    }

    private static Vector3 Cross(Vector3 left, Vector3 right) => new(
        (left.Y * right.Z) - (left.Z * right.Y),
        (left.Z * right.X) - (left.X * right.Z),
        (left.X * right.Y) - (left.Y * right.X));

    private static double Dot(Vector3 left, Vector3 right)
        => (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);

    private static double Span(double minimum, double maximum)
    {
        if (minimum == maximum) return 0d;
        var scale = Math.Max(Math.Abs(minimum), Math.Abs(maximum));
        if (scale == 0d) return 0d;
        return Math.Abs((maximum / scale) - (minimum / scale)) * scale;
    }

    private static double Midpoint(double minimum, double maximum)
        => (minimum * 0.5d) + (maximum * 0.5d);

    private static double Hypot(double x, double y)
    {
        var ax = Math.Abs(x);
        var ay = Math.Abs(y);
        var scale = Math.Max(ax, ay);
        if (scale == 0d) return 0d;
        if (!Numeric.IsFinite(scale)) return double.PositiveInfinity;
        var sx = x / scale;
        var sy = y / scale;
        return scale * Math.Sqrt((sx * sx) + (sy * sy));
    }

    private readonly record struct PixelPoint(double X, double Y);
}

using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Domain;
using QS3D.Platform.Geometry;

namespace QS3D.Platform.InMemory;

public sealed class InMemorySpatialSelectionService : ICadSpatialSelectionService
{
    private const double OrientationTolerance = 1e-12d;
    private readonly ICadDatabase _database;

    public InMemorySpatialSelectionService(ICadDatabase database)
        => _database = database ?? throw new ArgumentNullException(nameof(database));

    public IReadOnlyList<CadHandle> SelectPolygon(IReadOnlyList<Point3> points, CadSelectionMode mode)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));
        if (!Enum.IsDefined(typeof(CadSelectionMode), mode))
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Selection mode must be a defined value.");
        if (points.Count < 3) throw new ArgumentException("Selection polygon must contain at least three points.", nameof(points));
        if (PolygonAreaMagnitude(points) <= OrientationTolerance)
            throw new ArgumentException("Selection polygon must enclose a non-zero XY area.", nameof(points));

        using var tx = _database.BeginTransaction(CadTransactionMode.ReadOnly);
        return tx.Query()
            .Where(entity => Selects(entity.Extents, points, mode))
            .Select(static entity => entity.Handle)
            .OrderBy(static handle => handle)
            .ToArray();
    }

    private static bool Selects(BoundingBox3 bounds, IReadOnlyList<Point3> polygon, CadSelectionMode mode)
    {
        switch (mode)
        {
            case CadSelectionMode.Window:
                return BoxCorners(bounds).All(point => PointInPolygon(point, polygon));
            case CadSelectionMode.Crossing:
            case CadSelectionMode.Lasso:
                return BoxIntersectsPolygon(bounds, polygon);
            case CadSelectionMode.Fence:
                return FenceIntersectsBox(bounds, polygon);
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported spatial selection mode.");
        }
    }

    private static bool BoxIntersectsPolygon(BoundingBox3 bounds, IReadOnlyList<Point3> polygon)
    {
        var corners = BoxCorners(bounds);
        if (corners.Any(point => PointInPolygon(point, polygon))) return true;
        if (polygon.Any(point => PointInBox(point, bounds))) return true;
        return PolygonSegments(polygon).Any(segment => BoxEdges(bounds).Any(edge => SegmentsIntersect(segment.A, segment.B, edge.A, edge.B)));
    }

    private static bool FenceIntersectsBox(BoundingBox3 bounds, IReadOnlyList<Point3> polygon)
    {
        foreach (var segment in PolygonSegments(polygon))
        {
            if (PointInBox(segment.A, bounds) || PointInBox(segment.B, bounds)) return true;
            if (BoxEdges(bounds).Any(edge => SegmentsIntersect(segment.A, segment.B, edge.A, edge.B))) return true;
        }
        return false;
    }

    private static Point3[] BoxCorners(BoundingBox3 bounds) => new[]
    {
        new Point3(bounds.Min.X, bounds.Min.Y),
        new Point3(bounds.Min.X, bounds.Max.Y),
        new Point3(bounds.Max.X, bounds.Min.Y),
        new Point3(bounds.Max.X, bounds.Max.Y)
    };

    private static Segment[] BoxEdges(BoundingBox3 bounds)
    {
        var corners = BoxCorners(bounds);
        return new[]
        {
            new Segment(corners[0], corners[1]),
            new Segment(corners[1], corners[3]),
            new Segment(corners[3], corners[2]),
            new Segment(corners[2], corners[0])
        };
    }

    private static IEnumerable<Segment> PolygonSegments(IReadOnlyList<Point3> polygon)
    {
        for (var index = 0; index < polygon.Count; index++)
            yield return new Segment(polygon[index], polygon[(index + 1) % polygon.Count]);
    }

    private static bool PointInBox(Point3 point, BoundingBox3 bounds)
        => point.X >= bounds.Min.X && point.X <= bounds.Max.X
            && point.Y >= bounds.Min.Y && point.Y <= bounds.Max.Y;

    private static bool PointInPolygon(Point3 point, IReadOnlyList<Point3> polygon)
    {
        var scaleX = Math.Max(Math.Abs(point.X), polygon.Max(static p => Math.Abs(p.X)));
        var scaleY = Math.Max(Math.Abs(point.Y), polygon.Max(static p => Math.Abs(p.Y)));
        if (scaleX == 0d) scaleX = 1d;
        if (scaleY == 0d) scaleY = 1d;
        var px = point.X / scaleX;
        var py = point.Y / scaleY;
        var inside = false;
        for (var index = 0; index < polygon.Count; index++)
        {
            var a = polygon[index];
            var b = polygon[(index + 1) % polygon.Count];
            if (OnSegment(a, point, b)) return true;
            var ay = a.Y / scaleY;
            var by = b.Y / scaleY;
            if ((ay > py) == (by > py)) continue;
            var ax = a.X / scaleX;
            var bx = b.X / scaleX;
            var x = ax + ((py - ay) * (bx - ax) / (by - ay));
            if (x >= px) inside = !inside;
        }
        return inside;
    }

    private static bool SegmentsIntersect(Point3 a, Point3 b, Point3 c, Point3 d)
    {
        var o1 = Orientation(a, b, c);
        var o2 = Orientation(a, b, d);
        var o3 = Orientation(c, d, a);
        var o4 = Orientation(c, d, b);
        if (o1 != o2 && o3 != o4) return true;
        return (o1 == 0 && OnSegment(a, c, b))
            || (o2 == 0 && OnSegment(a, d, b))
            || (o3 == 0 && OnSegment(c, a, d))
            || (o4 == 0 && OnSegment(c, b, d));
    }

    private static int Orientation(Point3 a, Point3 b, Point3 c)
    {
        var scaleX = Math.Max(Math.Abs(a.X), Math.Max(Math.Abs(b.X), Math.Abs(c.X)));
        var scaleY = Math.Max(Math.Abs(a.Y), Math.Max(Math.Abs(b.Y), Math.Abs(c.Y)));
        if (scaleX == 0d) scaleX = 1d;
        if (scaleY == 0d) scaleY = 1d;
        var ax = a.X / scaleX;
        var ay = a.Y / scaleY;
        var bx = b.X / scaleX;
        var by = b.Y / scaleY;
        var cx = c.X / scaleX;
        var cy = c.Y / scaleY;
        var value = ((bx - ax) * (cy - ay)) - ((by - ay) * (cx - ax));
        if (Math.Abs(value) <= OrientationTolerance) return 0;
        return value > 0d ? 1 : -1;
    }

    private static bool OnSegment(Point3 a, Point3 point, Point3 b)
    {
        if (Orientation(a, b, point) != 0) return false;
        return point.X >= Math.Min(a.X, b.X) && point.X <= Math.Max(a.X, b.X)
            && point.Y >= Math.Min(a.Y, b.Y) && point.Y <= Math.Max(a.Y, b.Y);
    }

    private static double PolygonAreaMagnitude(IReadOnlyList<Point3> polygon)
    {
        var scaleX = polygon.Max(static point => Math.Abs(point.X));
        var scaleY = polygon.Max(static point => Math.Abs(point.Y));
        if (scaleX == 0d || scaleY == 0d) return 0d;
        var sum = 0d;
        for (var index = 0; index < polygon.Count; index++)
        {
            var current = polygon[index];
            var next = polygon[(index + 1) % polygon.Count];
            sum += ((current.X / scaleX) * (next.Y / scaleY)) - ((next.X / scaleX) * (current.Y / scaleY));
        }
        return Math.Abs(sum) * 0.5d;
    }

    private readonly record struct Segment(Point3 A, Point3 B);
}

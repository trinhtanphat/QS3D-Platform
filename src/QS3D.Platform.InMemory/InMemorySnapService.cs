using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Domain;
using QS3D.Platform.Geometry;

namespace QS3D.Platform.InMemory;

public sealed class InMemorySnapService : ICadSnapService
{
    private readonly ICadDatabase _database;
    private readonly InMemoryViewportService _viewport;

    public InMemorySnapService(ICadDatabase database, InMemoryViewportService viewport)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
    }

    public IReadOnlyList<CadSnapCandidate> Query(Point3 worldPoint, double aperturePixels, CadSnapKind enabledKinds)
    {
        if (!Numeric.IsFinite(aperturePixels) || aperturePixels < 0d)
            throw new ArgumentOutOfRangeException(nameof(aperturePixels));
        if (enabledKinds == CadSnapKind.None) return Array.Empty<CadSnapCandidate>();

        using var tx = _database.BeginTransaction(CadTransactionMode.ReadOnly);
        var candidates = new List<CadSnapCandidate>();
        foreach (var entity in tx.Query())
        {
            AddEntityCandidates(candidates, entity, worldPoint, aperturePixels, enabledKinds);
        }

        return candidates
            .OrderBy(static candidate => candidate.DistancePixels)
            .ThenBy(static candidate => candidate.Handle)
            .ThenBy(static candidate => candidate.Kind)
            .ToArray();
    }

    private void AddEntityCandidates(
        ICollection<CadSnapCandidate> output,
        CadEntitySnapshot entity,
        Point3 query,
        double aperturePixels,
        CadSnapKind enabledKinds)
    {
        if ((enabledKinds & CadSnapKind.Endpoint) != 0 && entity.Kind == CadEntityKind.Line)
        {
            Add(output, entity.Handle, CadSnapKind.Endpoint, entity.Extents.Min, query, aperturePixels);
            Add(output, entity.Handle, CadSnapKind.Endpoint, entity.Extents.Max, query, aperturePixels);
        }

        if ((enabledKinds & CadSnapKind.Midpoint) != 0 && entity.Kind == CadEntityKind.Line)
        {
            Add(output, entity.Handle, CadSnapKind.Midpoint, Midpoint(entity.Extents), query, aperturePixels);
        }

        if ((enabledKinds & CadSnapKind.Center) != 0 && IsCenterEntity(entity.Kind))
        {
            Add(output, entity.Handle, CadSnapKind.Center, Midpoint(entity.Extents), query, aperturePixels);
        }

        if ((enabledKinds & CadSnapKind.Quadrant) != 0 && IsCenterEntity(entity.Kind))
        {
            var center = Midpoint(entity.Extents);
            Add(output, entity.Handle, CadSnapKind.Quadrant, new Point3(entity.Extents.Min.X, center.Y, center.Z), query, aperturePixels);
            Add(output, entity.Handle, CadSnapKind.Quadrant, new Point3(entity.Extents.Max.X, center.Y, center.Z), query, aperturePixels);
            Add(output, entity.Handle, CadSnapKind.Quadrant, new Point3(center.X, entity.Extents.Min.Y, center.Z), query, aperturePixels);
            Add(output, entity.Handle, CadSnapKind.Quadrant, new Point3(center.X, entity.Extents.Max.Y, center.Z), query, aperturePixels);
        }

        if ((enabledKinds & CadSnapKind.Nearest) != 0)
        {
            Add(output, entity.Handle, CadSnapKind.Nearest, ClosestPoint(entity.Extents, query), query, aperturePixels);
        }
    }

    private void Add(
        ICollection<CadSnapCandidate> output,
        CadHandle handle,
        CadSnapKind kind,
        Point3 point,
        Point3 query,
        double aperturePixels)
    {
        var distance = _viewport.PixelDistance(query, point);
        if (Numeric.IsFinite(distance) && distance <= aperturePixels)
            output.Add(new CadSnapCandidate(handle, kind, point, distance));
    }

    private static bool IsCenterEntity(CadEntityKind kind)
        => kind == CadEntityKind.Circle || kind == CadEntityKind.Arc || kind == CadEntityKind.Ellipse;

    private static Point3 Midpoint(BoundingBox3 bounds) => new(
        Midpoint(bounds.Min.X, bounds.Max.X),
        Midpoint(bounds.Min.Y, bounds.Max.Y),
        Midpoint(bounds.Min.Z, bounds.Max.Z));

    private static double Midpoint(double minimum, double maximum)
        => (minimum * 0.5d) + (maximum * 0.5d);

    private static Point3 ClosestPoint(BoundingBox3 bounds, Point3 point) => new(
        Math.Max(bounds.Min.X, Math.Min(bounds.Max.X, point.X)),
        Math.Max(bounds.Min.Y, Math.Min(bounds.Max.Y, point.Y)),
        Math.Max(bounds.Min.Z, Math.Min(bounds.Max.Z, point.Z)));
}

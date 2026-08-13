using System.Runtime.CompilerServices;
using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Geometry;
using QS3D.Platform.InMemory;

namespace QS3D.Platform.SmokeTests;

internal static class InMemoryViewportSnapModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var database = new InMemoryCadDatabase();
        CadHandle line;
        CadHandle circle;
        using (var tx = database.BeginTransaction())
        {
            line = tx.Append(new CadEntityDraft(CadEntityKind.Line, new BoundingBox3(new Point3(0, 0), new Point3(10, 0))));
            circle = tx.Append(new CadEntityDraft(CadEntityKind.Circle, new BoundingBox3(new Point3(-2, -2), new Point3(2, 2))));
            tx.Commit();
        }

        var viewport = new InMemoryViewportService(database);
        var hits = viewport.HitTest(new Point3(5, 1), 11d);
        Require(hits.Any(hit => hit.Handle == line), "hit-test must include the line within 10 projected pixels");

        viewport.ZoomExtents();
        Nearly(4d, viewport.CurrentView.Target.X, 1e-12d);
        Nearly(0d, viewport.CurrentView.Target.Y, 1e-12d);
        Nearly(12.6d, viewport.CurrentView.Width, 1e-12d);
        Nearly(4.2d, viewport.CurrentView.Height, 1e-12d);
        Throws<ArgumentException>(() => viewport.SetView(new CadViewState(
            new Point3(0, 0), new Vector3(0, 0, -1), new Vector3(0, 0, 1), 10d, 10d)));

        viewport.SetView(new CadViewState(
            new Point3(0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0), 100d, 100d));
        var snaps = new InMemorySnapService(database, viewport);
        var endpoint = snaps.Query(new Point3(0, 0), 1d, CadSnapKind.Endpoint);
        Require(endpoint.Any(candidate => candidate.Handle == line && candidate.Kind == CadSnapKind.Endpoint && candidate.Point.Equals(new Point3(0, 0))),
            "endpoint snap must expose line endpoint");

        var midpoint = snaps.Query(new Point3(5, 0), 1d, CadSnapKind.Midpoint);
        Require(midpoint.Any(candidate => candidate.Handle == line && candidate.Kind == CadSnapKind.Midpoint),
            "midpoint snap must expose line midpoint");

        var center = snaps.Query(new Point3(0, 0), 1d, CadSnapKind.Center);
        Require(center.Any(candidate => candidate.Handle == circle && candidate.Kind == CadSnapKind.Center),
            "center snap must expose circle center");

        var nearest = snaps.Query(new Point3(5, 1), 11d, CadSnapKind.Nearest);
        Require(nearest.Any(candidate => candidate.Handle == line && candidate.Kind == CadSnapKind.Nearest),
            "nearest snap must expose line AABB nearest point");

        Equal(0, snaps.Query(new Point3(0, 0), 100d, CadSnapKind.Intersection | CadSnapKind.Tangent).Count);

        Console.WriteLine("PASS in-memory viewport and snap reference services");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Nearly(double expected, double actual, double tolerance)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"Expected approximately {expected:R} but got {actual:R}.");
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

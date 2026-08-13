using System.Runtime.CompilerServices;
using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Geometry;
using QS3D.Platform.InMemory;

namespace QS3D.Platform.SmokeTests;

internal static class InMemorySpatialSelectionModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var database = new InMemoryCadDatabase();
        CadHandle first;
        CadHandle second;
        using (var tx = database.BeginTransaction())
        {
            first = tx.Append(new CadEntityDraft(CadEntityKind.Polyline, new BoundingBox3(new Point3(1, 1), new Point3(2, 2))));
            second = tx.Append(new CadEntityDraft(CadEntityKind.Polyline, new BoundingBox3(new Point3(8, 8), new Point3(9, 9))));
            tx.Commit();
        }

        var service = new InMemorySpatialSelectionService(database);
        var window = service.SelectPolygon(new[]
        {
            new Point3(0, 0), new Point3(5, 0), new Point3(5, 5), new Point3(0, 5)
        }, CadSelectionMode.Window);
        Equal(1, window.Count);
        Equal(first, window[0]);

        var crossing = service.SelectPolygon(new[]
        {
            new Point3(1.5, 0), new Point3(1.7, 0), new Point3(1.7, 10), new Point3(1.5, 10)
        }, CadSelectionMode.Crossing);
        Equal(1, crossing.Count);
        Equal(first, crossing[0]);

        var all = service.SelectPolygon(new[]
        {
            new Point3(-1, -1), new Point3(11, -1), new Point3(11, 11), new Point3(-1, 11)
        }, CadSelectionMode.Lasso);
        Equal(2, all.Count);
        Equal(first, all[0]);
        Equal(second, all[1]);

        Throws<ArgumentException>(() => service.SelectPolygon(new[]
        {
            new Point3(0, 0), new Point3(1, 0), new Point3(2, 0)
        }, CadSelectionMode.Window));

        Console.WriteLine("PASS in-memory spatial selection reference service");
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

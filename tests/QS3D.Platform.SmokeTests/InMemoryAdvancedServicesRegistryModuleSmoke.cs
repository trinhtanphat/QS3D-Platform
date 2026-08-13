using System.Runtime.CompilerServices;
using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Geometry;
using QS3D.Platform.InMemory;

namespace QS3D.Platform.SmokeTests;

internal static class InMemoryAdvancedServicesRegistryModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var firstDocument = new InMemoryCadDocument("First");
        var secondDocument = new InMemoryCadDocument("Second");

        var first = InMemoryAdvancedServicesRegistry.For(firstDocument);
        var again = InMemoryAdvancedServicesRegistry.For(firstDocument);
        var second = InMemoryAdvancedServicesRegistry.For(secondDocument);

        Require(ReferenceEquals(first, again), "same document must retain one advanced-service bundle");
        Require(!ReferenceEquals(first, second), "different documents must not share advanced-service state");

        first.Viewport.SetView(new CadViewState(
            new Point3(5, 6),
            new Vector3(0, 0, -1),
            new Vector3(0, 1, 0),
            25d,
            30d));

        Equal(new Point3(5, 6), again.Viewport.CurrentView.Target);
        Equal(new Point3(0, 0), second.Viewport.CurrentView.Target);

        using (var tx = firstDocument.Database.BeginTransaction())
        {
            tx.Append(new CadEntityDraft(
                CadEntityKind.Line,
                new BoundingBox3(new Point3(0, 0), new Point3(10, 0))));
            tx.Commit();
        }

        var snaps = first.Snaps.Query(new Point3(0, 0), 100d, CadSnapKind.Endpoint);
        Require(snaps.Count != 0, "registry snap service must observe document database state");

        Console.WriteLine("PASS document-scoped advanced service registry");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }
}

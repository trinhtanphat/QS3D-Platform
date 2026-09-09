using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Persistence;

namespace QS3D.Platform.SmokeTests;

internal static class SemanticSnapshotReadonlyListsModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var floor = new FloorSnapshot(Guid.NewGuid(), "L1", 0d);
        var zone = new ZoneSnapshot(Guid.NewGuid(), "Core");
        var family = new FamilySnapshot(Guid.NewGuid(), SemanticElementKind.Wall, "Wall");
        var generated = new CadReferenceSnapshot(Guid.NewGuid(), "1A");
        var element = new ElementSnapshot(
            Guid.NewGuid(),
            SemanticElementKind.Wall,
            "W1",
            family.Id,
            floor.Id,
            zone.Id,
            null,
            new[] { generated },
            null);

        var snapshot = new SemanticProjectSnapshot(
            1,
            Guid.NewGuid(),
            "Readonly",
            new[] { floor },
            new[] { zone },
            new[] { family },
            new[] { element });

        AssertReadOnly(snapshot.Floors, new FloorSnapshot(Guid.NewGuid(), "L2", 3d), "floors");
        AssertReadOnly(snapshot.Zones, new ZoneSnapshot(Guid.NewGuid(), "Perimeter"), "zones");
        AssertReadOnly(snapshot.Families, new FamilySnapshot(Guid.NewGuid(), SemanticElementKind.Column, "Column"), "families");
        AssertReadOnly(snapshot.Elements, new ElementSnapshot(
            Guid.NewGuid(),
            SemanticElementKind.Wall,
            "W2",
            family.Id,
            floor.Id,
            zone.Id,
            null,
            Array.Empty<CadReferenceSnapshot>(),
            null), "elements");
        AssertReadOnly(element.GeneratedReferences, new CadReferenceSnapshot(Guid.NewGuid(), "2B"), "generated references");

        Console.WriteLine("PASS semantic persistence snapshot list immutability");
    }

    private static void AssertReadOnly<T>(IReadOnlyList<T> values, T replacement, string label) where T : class
    {
        if (values.Count != 1) throw new InvalidOperationException($"Expected one {label} entry for immutability regression.");
        var original = values[0];
        if (values is not IList<T> mutable)
            throw new InvalidOperationException($"{label} should expose standard indexed read semantics through IList<T>.");

        Throws<NotSupportedException>(() => mutable[0] = replacement);
        if (!ReferenceEquals(original, values[0]))
            throw new InvalidOperationException($"Semantic snapshot {label} changed after a rejected mutation.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}

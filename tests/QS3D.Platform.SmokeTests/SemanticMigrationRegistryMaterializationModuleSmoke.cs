using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Persistence;

namespace QS3D.Platform.SmokeTests;

internal static class SemanticMigrationRegistryMaterializationModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        Throws<ArgumentException>(() => new SemanticSnapshotMigrator(new AdvertisedOnlyCollection<ISemanticSnapshotMigration>(257)));
        Throws<ArgumentException>(() => new SemanticSnapshotMigrator(new OverrunCollection<ISemanticSnapshotMigration>(new Step(1, 2), new Step(2, 3))));
        Throws<ArgumentException>(() => new SemanticSnapshotMigrator(new PostTraversalCountDriftCollection<ISemanticSnapshotMigration>(new Step(1, 2))));
        Console.WriteLine("PASS semantic migration registry bounded materialization contracts");
    }

    private sealed class Step : ISemanticSnapshotMigration
    {
        public Step(int from, int to) { FromVersion = from; ToVersion = to; }
        public int FromVersion { get; }
        public int ToVersion { get; }
        public SemanticProjectSnapshot Apply(SemanticProjectSnapshot source) => source;
    }

    private sealed class AdvertisedOnlyCollection<T> : IReadOnlyCollection<T>
    {
        public AdvertisedOnlyCollection(int count) => Count = count;
        public int Count { get; }
        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException("Oversized registry must be rejected before traversal.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class OverrunCollection<T> : IReadOnlyCollection<T>
    {
        private readonly T _first;
        private readonly T _second;
        public OverrunCollection(T first, T second) { _first = first; _second = second; }
        public int Count => 1;
        public IEnumerator<T> GetEnumerator() { yield return _first; yield return _second; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class PostTraversalCountDriftCollection<T> : IReadOnlyCollection<T>
    {
        private readonly T _item;
        private bool _traversed;
        public PostTraversalCountDriftCollection(T item) => _item = item;
        public int Count => _traversed ? 2 : 1;
        public IEnumerator<T> GetEnumerator() { yield return _item; _traversed = true; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}

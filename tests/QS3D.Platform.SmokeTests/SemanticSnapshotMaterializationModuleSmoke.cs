using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Persistence;

namespace QS3D.Platform.SmokeTests;

internal static class SemanticSnapshotMaterializationModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var family = new FamilySnapshot(Guid.NewGuid(), SemanticElementKind.Wall, "Wall");

        Throws<ArgumentException>(() => new SemanticProjectSnapshot(
            1,
            Guid.NewGuid(),
            "Oversized",
            new AdvertisedOnlyCollection<FloorSnapshot>(100_001),
            Array.Empty<ZoneSnapshot>(),
            new[] { family },
            Array.Empty<ElementSnapshot>()));

        Throws<ArgumentException>(() => new SemanticProjectSnapshot(
            1,
            Guid.NewGuid(),
            "Overrun",
            new OverrunCollection<FloorSnapshot>(
                new FloorSnapshot(Guid.NewGuid(), "L1", 0d),
                new FloorSnapshot(Guid.NewGuid(), "L2", 3d)),
            Array.Empty<ZoneSnapshot>(),
            new[] { family },
            Array.Empty<ElementSnapshot>()));

        Throws<ArgumentException>(() => new SemanticProjectSnapshot(
            1,
            Guid.NewGuid(),
            "Drift",
            new PostTraversalCountDriftCollection<FloorSnapshot>(new FloorSnapshot(Guid.NewGuid(), "L1", 0d)),
            Array.Empty<ZoneSnapshot>(),
            new[] { family },
            Array.Empty<ElementSnapshot>()));

        Throws<ArgumentException>(() => new ElementSnapshot(
            Guid.NewGuid(),
            SemanticElementKind.Wall,
            "E1",
            family.Id,
            null,
            null,
            null,
            Array.Empty<CadReferenceSnapshot>(),
            new PostTraversalCountDriftDictionary("ThicknessMm", "200")));

        Console.WriteLine("PASS semantic snapshot bounded materialization contracts");
    }

    private sealed class AdvertisedOnlyCollection<T> : IReadOnlyCollection<T>
    {
        public AdvertisedOnlyCollection(int count) => Count = count;
        public int Count { get; }
        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException("Oversized collection must be rejected before traversal.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class OverrunCollection<T> : IReadOnlyCollection<T>
    {
        private readonly T _first;
        private readonly T _second;

        public OverrunCollection(T first, T second)
        {
            _first = first;
            _second = second;
        }

        public int Count => 1;

        public IEnumerator<T> GetEnumerator()
        {
            yield return _first;
            yield return _second;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class PostTraversalCountDriftCollection<T> : IReadOnlyCollection<T>
    {
        private readonly T _item;
        private bool _traversed;

        public PostTraversalCountDriftCollection(T item) => _item = item;

        public int Count => _traversed ? 2 : 1;

        public IEnumerator<T> GetEnumerator()
        {
            yield return _item;
            _traversed = true;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class PostTraversalCountDriftDictionary : IReadOnlyDictionary<string, string>
    {
        private readonly KeyValuePair<string, string> _pair;
        private bool _traversed;

        public PostTraversalCountDriftDictionary(string key, string value)
            => _pair = new KeyValuePair<string, string>(key, value);

        public int Count => _traversed ? 2 : 1;
        public IEnumerable<string> Keys => new[] { _pair.Key };
        public IEnumerable<string> Values => new[] { _pair.Value };
        public string this[string key] => key == _pair.Key ? _pair.Value : throw new KeyNotFoundException();
        public bool ContainsKey(string key) => key == _pair.Key;
        public bool TryGetValue(string key, out string value)
        {
            if (key == _pair.Key)
            {
                value = _pair.Value;
                return true;
            }

            value = string.Empty;
            return false;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            yield return _pair;
            _traversed = true;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}

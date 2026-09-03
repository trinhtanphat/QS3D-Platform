using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Persistence;

namespace QS3D.Platform.SmokeTests;

internal static class ProjectContainerManifestCardinalityModuleSmoke
{
    private const int ExpectedLimit = 100_000;

    [ModuleInitializer]
    internal static void Run()
    {
        RejectsOversizedAdvertisedCountBeforeTraversal();
        RejectsEnumerationOverrun();
        RejectsCountEnumerationMismatch();
        RejectsPostTraversalCountDrift();
        RejectsConflictingCountInterfaces();
        PreservesValidManifestSemantics();
        Console.WriteLine("PASS project container manifest payload cardinality contracts");
    }

    private static void RejectsOversizedAdvertisedCountBeforeTraversal()
    {
        var source = new AdvertisedCountCollection(ExpectedLimit + 1, throwOnEnumeration: true);
        Throws<ArgumentException>(() => CreateManifest(source));
        if (source.EnumerationStarted)
            throw new InvalidOperationException("Oversized advertised Count must fail before payload traversal.");
    }

    private static void RejectsEnumerationOverrun()
    {
        Throws<ArgumentException>(() => CreateManifest(new OverrunEnumerable(ExpectedLimit + 1)));
    }

    private static void RejectsCountEnumerationMismatch()
    {
        var semantic = SemanticPayload();
        Throws<ArgumentException>(() => CreateManifest(new AdvertisedCountCollection(2, semantic)));
    }

    private static void RejectsPostTraversalCountDrift()
    {
        Throws<ArgumentException>(() => CreateManifest(new DriftingCountCollection(SemanticPayload())));
    }

    private static void RejectsConflictingCountInterfaces()
    {
        Throws<ArgumentException>(() => CreateManifest(new ConflictingCountCollection(SemanticPayload())));
    }

    private static void PreservesValidManifestSemantics()
    {
        var semantic = SemanticPayload(length: 5);
        var drawing = new ProjectContainerPayload(ProjectContainerSectionNames.DrawingPayload, "application/octet-stream", 7, new string('B', 64));
        var manifest = CreateManifest(new[] { semantic, drawing });

        if (manifest.Payloads.Count != 2)
            throw new InvalidOperationException("Valid payloads were not preserved.");
        if (!StringComparer.Ordinal.Equals(manifest.Payloads[0].Name, ProjectContainerSectionNames.DrawingPayload)
            || !StringComparer.Ordinal.Equals(manifest.Payloads[1].Name, ProjectContainerSectionNames.SemanticProject))
            throw new InvalidOperationException("Manifest payload ordering changed unexpectedly.");
        if (manifest.TotalDeclaredBytes != 12)
            throw new InvalidOperationException("Manifest declared-byte total changed unexpectedly.");
    }

    private static ProjectContainerManifest CreateManifest(IEnumerable<ProjectContainerPayload> payloads) =>
        new(1, Guid.NewGuid(), payloads);

    private static ProjectContainerPayload SemanticPayload(long length = 1) =>
        new(ProjectContainerSectionNames.SemanticProject, "application/json", length, new string('A', 64));

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private sealed class AdvertisedCountCollection : ICollection<ProjectContainerPayload>
    {
        private readonly IReadOnlyList<ProjectContainerPayload> _items;
        private readonly bool _throwOnEnumeration;

        public AdvertisedCountCollection(int count, ProjectContainerPayload? item = null, bool throwOnEnumeration = false)
        {
            Count = count;
            _items = item is null ? Array.Empty<ProjectContainerPayload>() : new[] { item };
            _throwOnEnumeration = throwOnEnumeration;
        }

        public bool EnumerationStarted { get; private set; }
        public int Count { get; }
        public bool IsReadOnly => true;

        public IEnumerator<ProjectContainerPayload> GetEnumerator()
        {
            EnumerationStarted = true;
            if (_throwOnEnumeration) throw new InvalidOperationException("Enumeration must not start.");
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(ProjectContainerPayload item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(ProjectContainerPayload item) => _items.Contains(item);
        public void CopyTo(ProjectContainerPayload[] array, int arrayIndex) => throw new NotSupportedException();
        public bool Remove(ProjectContainerPayload item) => throw new NotSupportedException();
    }

    private sealed class OverrunEnumerable : IEnumerable<ProjectContainerPayload>
    {
        private readonly int _count;
        public OverrunEnumerable(int count) => _count = count;

        public IEnumerator<ProjectContainerPayload> GetEnumerator()
        {
            yield return SemanticPayload();
            for (var index = 1; index < _count; index++)
                yield return new ProjectContainerPayload($"p{index}", "application/octet-stream", 0, new string('C', 64), required: false);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class DriftingCountCollection : ICollection<ProjectContainerPayload>
    {
        private readonly ProjectContainerPayload _item;
        private bool _enumerated;
        public DriftingCountCollection(ProjectContainerPayload item) => _item = item;
        public int Count => _enumerated ? 2 : 1;
        public bool IsReadOnly => true;

        public IEnumerator<ProjectContainerPayload> GetEnumerator()
        {
            yield return _item;
            _enumerated = true;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(ProjectContainerPayload item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(ProjectContainerPayload item) => ReferenceEquals(item, _item);
        public void CopyTo(ProjectContainerPayload[] array, int arrayIndex) => throw new NotSupportedException();
        public bool Remove(ProjectContainerPayload item) => throw new NotSupportedException();
    }

    private sealed class ConflictingCountCollection : ICollection<ProjectContainerPayload>, IReadOnlyCollection<ProjectContainerPayload>
    {
        private readonly ProjectContainerPayload _item;
        public ConflictingCountCollection(ProjectContainerPayload item) => _item = item;
        int ICollection<ProjectContainerPayload>.Count => 1;
        int IReadOnlyCollection<ProjectContainerPayload>.Count => 2;
        public bool IsReadOnly => true;

        public IEnumerator<ProjectContainerPayload> GetEnumerator()
        {
            yield return _item;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(ProjectContainerPayload item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(ProjectContainerPayload item) => ReferenceEquals(item, _item);
        public void CopyTo(ProjectContainerPayload[] array, int arrayIndex) => throw new NotSupportedException();
        public bool Remove(ProjectContainerPayload item) => throw new NotSupportedException();
    }
}

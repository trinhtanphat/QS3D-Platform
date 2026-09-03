using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Persistence;

namespace QS3D.Platform.SmokeTests;

internal static class ProjectContainerPayloadSetCardinalityModuleSmoke
{
    private const int ExpectedLimit = 100_000;

    [ModuleInitializer]
    internal static void Run()
    {
        RejectsOversizedAdvertisedCountBeforeTraversal();
        RejectsEnumerationOverrun();
        RejectsCountEnumerationMismatch();
        RejectsPostTraversalCountDrift();
        PreservesValidPayloadSetValidation();
        Console.WriteLine("PASS project container payload-set cardinality contracts");
    }

    private static void RejectsOversizedAdvertisedCountBeforeTraversal()
    {
        var source = new HostilePayloadDictionary(
            () => ExpectedLimit + 1,
            () => throw new InvalidOperationException("Enumeration must not start."));

        Throws<ArgumentException>(() => ProjectContainerManifestValidator.ValidatePayloadSet(CreateManifest(), source));
        if (source.EnumerationStarted)
            throw new InvalidOperationException("Oversized advertised Count must fail before payload traversal.");
    }

    private static void RejectsEnumerationOverrun()
    {
        var source = new HostilePayloadDictionary(
            () => ExpectedLimit,
            () => EnumerateOverLimit());

        Throws<ArgumentException>(() => ProjectContainerManifestValidator.ValidatePayloadSet(CreateManifest(), source));
    }

    private static void RejectsCountEnumerationMismatch()
    {
        var bytes = SemanticBytes();
        var source = new HostilePayloadDictionary(
            () => 2,
            () => new[] { Pair(ProjectContainerSectionNames.SemanticProject, bytes) });

        Throws<ArgumentException>(() => ProjectContainerManifestValidator.ValidatePayloadSet(CreateManifest(bytes), source));
    }

    private static void RejectsPostTraversalCountDrift()
    {
        var bytes = SemanticBytes();
        var enumerated = false;
        var source = new HostilePayloadDictionary(
            () => enumerated ? 2 : 1,
            () => EnumerateAndMark());

        Throws<ArgumentException>(() => ProjectContainerManifestValidator.ValidatePayloadSet(CreateManifest(bytes), source));

        IEnumerable<KeyValuePair<string, byte[]>> EnumerateAndMark()
        {
            yield return Pair(ProjectContainerSectionNames.SemanticProject, bytes);
            enumerated = true;
        }
    }

    private static void PreservesValidPayloadSetValidation()
    {
        var semantic = SemanticBytes();
        var drawing = new byte[] { 4, 5, 6, 7 };
        var manifest = new ProjectContainerManifest(
            1,
            Guid.NewGuid(),
            new[]
            {
                new ProjectContainerPayload(ProjectContainerSectionNames.SemanticProject, "application/json", semantic.LongLength, ProjectContainerManifest.Hash(semantic)),
                new ProjectContainerPayload(ProjectContainerSectionNames.DrawingPayload, "application/octet-stream", drawing.LongLength, ProjectContainerManifest.Hash(drawing), required: false)
            });

        var source = new HostilePayloadDictionary(
            () => 2,
            () => new[]
            {
                Pair(" SEMANTIC-PROJECT ", semantic),
                Pair(ProjectContainerSectionNames.DrawingPayload, drawing)
            });

        ProjectContainerManifestValidator.ValidatePayloadSet(manifest, source);
    }

    private static IEnumerable<KeyValuePair<string, byte[]>> EnumerateOverLimit()
    {
        yield return Pair(ProjectContainerSectionNames.SemanticProject, SemanticBytes());
        for (var index = 1; index < ExpectedLimit + 1; index++)
            yield return Pair($"payload-{index}", Array.Empty<byte>());
    }

    private static ProjectContainerManifest CreateManifest(byte[]? semantic = null)
    {
        semantic ??= SemanticBytes();
        return new ProjectContainerManifest(
            1,
            Guid.NewGuid(),
            new[]
            {
                new ProjectContainerPayload(
                    ProjectContainerSectionNames.SemanticProject,
                    "application/json",
                    semantic.LongLength,
                    ProjectContainerManifest.Hash(semantic))
            });
    }

    private static byte[] SemanticBytes() => new byte[] { 1, 2, 3 };

    private static KeyValuePair<string, byte[]> Pair(string name, byte[] bytes) => new(name, bytes);

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private sealed class HostilePayloadDictionary : IReadOnlyDictionary<string, byte[]>
    {
        private readonly Func<int> _count;
        private readonly Func<IEnumerable<KeyValuePair<string, byte[]>>> _items;

        public HostilePayloadDictionary(Func<int> count, Func<IEnumerable<KeyValuePair<string, byte[]>>> items)
        {
            _count = count;
            _items = items;
        }

        public bool EnumerationStarted { get; private set; }
        public int Count => _count();
        public IEnumerable<string> Keys => Enumerate().Select(static pair => pair.Key);
        public IEnumerable<byte[]> Values => Enumerate().Select(static pair => pair.Value);
        public byte[] this[string key] => throw new NotSupportedException();
        public bool ContainsKey(string key) => throw new NotSupportedException();
        public bool TryGetValue(string key, out byte[] value)
        {
            value = Array.Empty<byte>();
            throw new NotSupportedException();
        }

        public IEnumerator<KeyValuePair<string, byte[]>> GetEnumerator() => Enumerate().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<KeyValuePair<string, byte[]>> Enumerate()
        {
            EnumerationStarted = true;
            return _items();
        }
    }
}

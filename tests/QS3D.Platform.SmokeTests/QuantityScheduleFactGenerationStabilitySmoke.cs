using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleFactGenerationStabilitySmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        SameCountReplacementIsRejected();
        Console.WriteLine("PASS quantity schedule fact generation stability");
    }

    private static void SameCountReplacementIsRejected()
    {
        var project = CreateProject(out var element);
        var first = new QuantityFact(
            element.Id,
            "WALL.LENGTH",
            new QuantityValue(QuantityDimension.Length, 1d));
        var replacement = new QuantityFact(
            element.Id,
            "WALL.LENGTH",
            new QuantityValue(QuantityDimension.Length, 9d));
        var source = new SameCountDriftCollection<QuantityFact>(
            new[] { first },
            new[] { replacement });

        try
        {
            _ = QuantityScheduleProjector.Project(project, source);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.IndexOf("content changed during materialization", StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            throw new InvalidOperationException(
                "same-count schedule fact replacement failed for the wrong reason: " + ex.Message,
                ex);
        }

        throw new InvalidOperationException("same-count schedule fact replacement was accepted unexpectedly.");
    }

    private static SemanticProject CreateProject(out SemanticElement element)
    {
        var project = new SemanticProject(
            new ProjectId(Guid.Parse("81000000-0000-0000-0000-000000000001")),
            "Schedule generation stability");
        var familyId = new FamilyId(Guid.Parse("81000000-0000-0000-0000-000000000002"));
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        element = new SemanticElement(
            new ElementId(Guid.Parse("81000000-0000-0000-0000-000000000003")),
            SemanticElementKind.Wall,
            "Wall A",
            familyId);
        project.AddElement(element);
        return project;
    }

    private sealed class SameCountDriftCollection<T> : ICollection<T>, IReadOnlyCollection<T>
    {
        private readonly T[] _first;
        private readonly T[] _second;
        private int _enumerations;

        internal SameCountDriftCollection(T[] first, T[] second)
        {
            _first = first ?? throw new ArgumentNullException(nameof(first));
            _second = second ?? throw new ArgumentNullException(nameof(second));
            if (_first.Length != _second.Length)
                throw new ArgumentException("Drift generations must preserve Count.");
        }

        public int Count => _first.Length;
        public bool IsReadOnly => true;

        public IEnumerator<T> GetEnumerator()
        {
            var generation = _enumerations++ == 0 ? _first : _second;
            return ((IEnumerable<T>)generation).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Contains(T item) => ((ICollection<T>)_first).Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _first.CopyTo(array, arrayIndex);
        public void Add(T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
    }
}

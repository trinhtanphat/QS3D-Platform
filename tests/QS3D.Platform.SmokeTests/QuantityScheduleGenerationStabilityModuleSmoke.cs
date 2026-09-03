using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleGenerationStabilityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        RejectSameCountRowReplacement();
        PreserveSinglePassStreamingRows();
        Console.WriteLine("PASS quantity schedule generation stability");
    }

    private static void RejectSameCountRowReplacement()
    {
        var elementId = ElementId.New();
        var familyId = FamilyId.New();
        var first = CreateRow(elementId, familyId, 1d);
        var replacement = CreateRow(elementId, familyId, 9d);
        var source = new SameCountDriftCollection<QuantityScheduleRow>(
            new[] { first },
            new[] { replacement });

        ExpectContentDrift(
            () => _ = new QuantitySchedule(source),
            "same-count quantity schedule row replacement");
    }

    private static void PreserveSinglePassStreamingRows()
    {
        var row = CreateRow(ElementId.New(), FamilyId.New(), 2d);
        var source = new SinglePassEnumerable<QuantityScheduleRow>(new[] { row });
        var schedule = new QuantitySchedule(source);
        if (schedule.Rows.Count != 1 || source.EnumerationCount != 1)
            throw new InvalidOperationException("raw streaming schedule rows lost single-pass semantics.");
    }

    private static QuantityScheduleRow CreateRow(ElementId elementId, FamilyId familyId, double value) =>
        new(
            elementId,
            "Wall",
            SemanticElementKind.Wall,
            familyId,
            "Wall Family",
            null,
            null,
            new[] { new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, value, 1, 1) });

    private static void ExpectContentDrift(Action action, string scenario)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.IndexOf("content changed during materialization", StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            throw new InvalidOperationException(scenario + " failed for the wrong reason: " + ex.Message, ex);
        }

        throw new InvalidOperationException(scenario + " was accepted unexpectedly.");
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

    private sealed class SinglePassEnumerable<T> : IEnumerable<T>
    {
        private readonly T[] _items;
        internal SinglePassEnumerable(T[] items) => _items = items ?? throw new ArgumentNullException(nameof(items));
        internal int EnumerationCount { get; private set; }
        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            if (EnumerationCount != 1)
                throw new InvalidOperationException("streaming input was enumerated more than once.");
            return ((IEnumerable<T>)_items).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

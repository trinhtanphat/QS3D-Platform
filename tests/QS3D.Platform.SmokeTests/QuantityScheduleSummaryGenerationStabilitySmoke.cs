using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleSummaryGenerationStabilitySmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        RejectSameCountReplacement();
        RejectSameCountReorder();
        PreserveSinglePassStreamingInput();
        Console.WriteLine("PASS quantity schedule summary generation stability");
    }

    private static void RejectSameCountReplacement()
    {
        var source = new SameCountDriftCollection<QuantitySummary>(
            new[] { new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 2d, 1, 1) },
            new[] { new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 9d, 1, 1) });

        ExpectContentDrift(() => _ = CreateRow(source), "same-count quantity-summary replacement");
    }

    private static void RejectSameCountReorder()
    {
        var first = new QuantitySummary("A.LENGTH", QuantityDimension.Length, 1d, 1, 1);
        var second = new QuantitySummary("B.LENGTH", QuantityDimension.Length, 2d, 1, 1);
        var source = new SameCountDriftCollection<QuantitySummary>(
            new[] { first, second },
            new[] { second, first });

        ExpectContentDrift(() => _ = CreateRow(source), "same-count quantity-summary reorder");
    }

    private static void PreserveSinglePassStreamingInput()
    {
        var source = new SinglePassEnumerable<QuantitySummary>(new[]
        {
            new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 2d, 1, 1)
        });

        var row = CreateRow(source);
        if (row.Quantities.Count != 1 || row.Quantities[0].Quantity.Value != 2d)
            throw new InvalidOperationException("streaming schedule summaries were not preserved.");
        if (source.EnumerationCount != 1)
            throw new InvalidOperationException("raw streaming schedule summaries were enumerated more than once.");
    }

    private static QuantityScheduleRow CreateRow(IEnumerable<QuantitySummary> quantities) =>
        new(
            ElementId.New(),
            "Wall A",
            SemanticElementKind.Wall,
            FamilyId.New(),
            "Wall Family",
            null,
            null,
            quantities);

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
            if (_first.Length != _second.Length) throw new ArgumentException("Drift generations must preserve Count.");
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
            if (EnumerationCount != 1) throw new InvalidOperationException("streaming input was enumerated more than once.");
            return ((IEnumerable<T>)_items).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqGenerationStabilitySmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        RejectRateReplacement();
        RejectQuantityReplacement();
        RejectBoqLineReplacement();
        RejectSameCountReorder();
        PreserveSinglePassStreamingInputs();
        Console.WriteLine("PASS BOQ commercial generation stability");
    }

    private static void RejectRateReplacement()
    {
        var quantity = new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 2d, 1, 1);
        var source = new SameCountDriftCollection<UnitRate>(
            new[] { new UnitRate("WALL.LENGTH", QuantityDimension.Length, 1m, "USD") },
            new[] { new UnitRate("WALL.LENGTH", QuantityDimension.Length, 9m, "USD") });

        ExpectContentDrift(
            () => _ = BoqProjector.Project(new[] { quantity }, source, "USD"),
            "same-count BOQ rate replacement");
    }

    private static void RejectQuantityReplacement()
    {
        var source = new SameCountDriftCollection<QuantitySummary>(
            new[] { new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 2d, 1, 1) },
            new[] { new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 7d, 1, 1) });
        var rates = new[] { new UnitRate("WALL.LENGTH", QuantityDimension.Length, 3m, "USD") };

        ExpectContentDrift(
            () => _ = BoqProjector.Project(source, rates, "USD"),
            "same-count BOQ quantity replacement");
    }

    private static void RejectBoqLineReplacement()
    {
        var source = new SameCountDriftCollection<BoqLine>(
            new[] { CreateLine("WALL.LENGTH", 2d, 1, 1, 3m) },
            new[] { CreateLine("WALL.LENGTH", 2d, 1, 1, 9m) });

        ExpectContentDrift(
            () => _ = new BoqProjection(source, "USD"),
            "same-count BQ line replacement");
    }

    private static void RejectSameCountReorder()
    {
        var first = new UnitRate("A.LENGTH", QuantityDimension.Length, 1m, "USD");
        var second = new UnitRate("B.LENGTH", QuantityDimension.Length, 1m, "USD");
        var source = new SameCountDriftCollection<UnitRate>(
            new[] { first, second },
            new[] { second, first });
        var quantities = new[]
        {
            new QuantitySummary("A.LENGTH", QuantityDimension.Length, 1d, 1, 1),
            new QuantitySummary("B.LENGTH", QuantityDimension.Length, 1d, 1, 1)
        };

        ExpectContentDrift(
            () => _ = BoqProjector.Project(quantities, source, "USD"),
            "same-count BOQ rate reorder");
    }

    private static void PreserveSinglePassStreamingInputs()
    {
        var quantityStream = new SinglePassEnumerable<QuantitySummary>(new[]
        {
            new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 2d, 1, 1)
        });
        var rateStream = new SinglePassEnumerable<UnitRate>(new[]
        {
            new UnitRate("WALL.LENGTH", QuantityDimension.Length, 3m, "USD")
        });

        var projection = BoqProjector.Project(quantityStream, rateStream, "USD");
        if (projection.Lines.Count != 1 || projection.Total.Amount != 6m)
            throw new InvalidOperationException("raw streaming BOQ inputs lost single-pass projection semantics.");
        if (quantityStream.EnumerationCount != 1 || rateStream.EnumerationCount != 1)
            throw new InvalidOperationException("raw streaming BOQ inputs were enumerated more than once.");
    }

    private static BoqLine CreateLine(
        string code,
        double value,
        int factCount,
        int elementCount,
        decimal unitRate)
    {
        var quantity = new QuantityValue(QuantityDimension.Length, value);
        var total = checked((decimal)value * unitRate);
        return new BoqLine(code, quantity, factCount, elementCount, unitRate, new Money(total, "USD"));
    }

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

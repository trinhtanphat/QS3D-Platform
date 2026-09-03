using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqGenerationStabilitySmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var quantity = new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 2d, 1, 1);
        var firstRate = new UnitRate("WALL.LENGTH", QuantityDimension.Length, 1m, "USD");
        var replacementRate = new UnitRate("WALL.LENGTH", QuantityDimension.Length, 9m, "USD");
        var source = new SameCountDriftCollection<UnitRate>(
            new[] { firstRate },
            new[] { replacementRate });

        try
        {
            _ = BoqProjector.Project(new[] { quantity }, source, "USD");
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.IndexOf("content changed during materialization", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine("PASS BOQ commercial generation stability");
                return;
            }

            throw new InvalidOperationException("same-count BOQ rate replacement failed for the wrong reason: " + ex.Message, ex);
        }

        throw new InvalidOperationException("same-count BOQ rate replacement was accepted unexpectedly.");
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

using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityAccumulatorPostCountDriftModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var fact = new QuantityFact(ElementId.New(), "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d));
        var source = new DriftCollection<QuantityFact>(fact);
        try
        {
            QuantityAccumulator.Summarize(source);
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("PASS accumulator final Count drift rejected");
            return;
        }
        throw new InvalidOperationException("Accumulator accepted a collection whose Count changed after enumeration.");
    }

    private sealed class DriftCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
    {
        private readonly T _item;
        private int _count = 1;
        internal DriftCollection(T item) { _item = item; }
        public int Count => _count;
        public bool IsReadOnly => true;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        public IEnumerator<T> GetEnumerator()
        {
            yield return _item;
            _count = 2;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
        public void CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;
        void ICollection.CopyTo(Array array, int index) => array.SetValue(_item, index);
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
    }
}

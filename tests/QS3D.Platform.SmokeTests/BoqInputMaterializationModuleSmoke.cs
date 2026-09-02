using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqInputMaterializationModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var quantityValue = new QuantityValue(QuantityDimension.Length, 1d);
        var quantity = new QuantitySummary("Q", QuantityDimension.Length, 1d, 1, 1);
        var rate = new UnitRate("Q", QuantityDimension.Length, 2m, "USD");
        var line = new BoqLine("Q", quantityValue, 1, 2m, new Money(2m, "USD"));

        ExpectInvalidOperation(
            () => BoqProjector.Project(new[] { quantity }, new OversizedAdvertisedCollection<UnitRate>(rate), "USD"),
            "BOQ rates with an advertised Count above the supported ceiling must be rejected before traversal.");

        ExpectInvalidOperation(
            () => BoqProjector.Project(new PostTraversalCountDriftCollection<QuantitySummary>(quantity), new[] { rate }, "USD"),
            "BOQ quantity Count drift after traversal must be rejected.");

        ExpectInvalidOperation(
            () => new BoqProjection(new PostTraversalCountDriftCollection<BoqLine>(line), "USD"),
            "Direct BOQ line Count drift after traversal must be rejected.");

        ExpectInvalidOperation(
            () => BoqProjector.Project(new[] { quantity }, new ConflictingCountCollection<UnitRate>(rate), "USD"),
            "Conflicting Count evidence across collection interfaces must be rejected.");

        ExpectArgument(
            () => BoqProjector.Project(new[] { quantity }, new UnitRate[] { null! }, "USD"),
            "Null BOQ rates must fail with an explicit argument contract.");

        ExpectArgument(
            () => BoqProjector.Project(new QuantitySummary[] { null! }, new[] { rate }, "USD"),
            "Null BOQ quantity summaries must fail with an explicit argument contract.");

        Console.WriteLine("PASS BOQ input materialization bounds and Count stability");
    }

    private static void ExpectInvalidOperation(Action action, string message)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void ExpectArgument(Action action, string message)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }
        catch (NullReferenceException ex)
        {
            throw new InvalidOperationException(message, ex);
        }

        throw new InvalidOperationException(message);
    }

    private sealed class OversizedAdvertisedCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
    {
        private readonly T _item;
        internal OversizedAdvertisedCollection(T item) => _item = item;
        public int Count => 100_001;
        public bool IsReadOnly => true;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        public IEnumerator<T> GetEnumerator() { yield return _item; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;
        void ICollection.CopyTo(Array array, int index) => array.SetValue(_item, index);
        public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
    }

    private sealed class PostTraversalCountDriftCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
    {
        private readonly T _item;
        private int _count = 1;
        internal PostTraversalCountDriftCollection(T item) => _item = item;
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
        public void CopyTo(T[] array, int arrayIndex)
        {
            array[arrayIndex] = _item;
            _count = 2;
        }
        void ICollection.CopyTo(Array array, int index)
        {
            array.SetValue(_item, index);
            _count = 2;
        }
        public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
    }

    private sealed class ConflictingCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
    {
        private readonly T _item;
        internal ConflictingCountCollection(T item) => _item = item;
        int ICollection<T>.Count => 1;
        int IReadOnlyCollection<T>.Count => 2;
        int ICollection.Count => 1;
        public bool IsReadOnly => true;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        public IEnumerator<T> GetEnumerator() { yield return _item; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;
        void ICollection.CopyTo(Array array, int index) => array.SetValue(_item, index);
        public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
    }
}

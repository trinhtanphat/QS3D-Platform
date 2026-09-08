using System.Collections;
using QS3D.Platform.Quantity;

static class BoqKnownCountNoOverreadModuleSmoke
{
    public static void Run()
    {
        var rate = new UnitRate("WALL.LENGTH", QuantityDimension.Length, 10m, "USD");
        var summary = new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 1d, factCount: 1, elementCount: 1);
        var line = new BoqLine(
            "WALL.LENGTH",
            new QuantityValue(QuantityDimension.Length, 1d),
            factCount: 1,
            elementCount: 1,
            unitRate: 10m,
            total: new Money(10m, "USD"));

        var rates = new CountControlledCollection<UnitRate>(rate, advertisedCount: 1, yieldedCount: 2);
        Throws<InvalidOperationException>(() => BoqProjector.Project(new[] { summary }, rates, "USD"));
        Equal(2, rates.MoveNextCalls);
        Equal(1, rates.CurrentReads);
        Equal(1, rates.DisposeCalls);

        var quantities = new CountControlledCollection<QuantitySummary>(summary, advertisedCount: 1, yieldedCount: 2);
        Throws<InvalidOperationException>(() => BoqProjector.Project(quantities, new[] { rate }, "USD"));
        Equal(2, quantities.MoveNextCalls);
        Equal(1, quantities.CurrentReads);
        Equal(1, quantities.DisposeCalls);

        var lines = new CountControlledCollection<BoqLine>(line, advertisedCount: 1, yieldedCount: 2);
        Throws<InvalidOperationException>(() => new BoqProjection(lines, "USD"));
        Equal(2, lines.MoveNextCalls);
        Equal(1, lines.CurrentReads);
        Equal(1, lines.DisposeCalls);

        var zero = new CountControlledCollection<UnitRate>(rate, advertisedCount: 0, yieldedCount: 1);
        Throws<InvalidOperationException>(() => BoqProjector.Project(new[] { summary }, zero, "USD"));
        Equal(1, zero.MoveNextCalls);
        Equal(0, zero.CurrentReads);
        Equal(1, zero.DisposeCalls);

        var earlyEnd = new CountControlledCollection<UnitRate>(rate, advertisedCount: 2, yieldedCount: 1);
        Throws<InvalidOperationException>(() => BoqProjector.Project(new[] { summary }, earlyEnd, "USD"));
        Equal(2, earlyEnd.MoveNextCalls);
        Equal(1, earlyEnd.CurrentReads);
        Equal(1, earlyEnd.DisposeCalls);
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private sealed class CountControlledCollection<T> : ICollection<T>
    {
        private readonly T _value;
        private readonly int _yieldedCount;

        public CountControlledCollection(T value, int advertisedCount, int yieldedCount)
        {
            _value = value;
            Count = advertisedCount;
            _yieldedCount = yieldedCount;
        }

        public int Count { get; }
        public bool IsReadOnly => true;
        public int MoveNextCalls { get; private set; }
        public int CurrentReads { get; private set; }
        public int DisposeCalls { get; private set; }

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Contains(T item) => EqualityComparer<T>.Default.Equals(item, _value);
        public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
        public void Add(T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();

        private sealed class Enumerator : IEnumerator<T>
        {
            private readonly CountControlledCollection<T> _owner;
            private int _index;

            public Enumerator(CountControlledCollection<T> owner) => _owner = owner;

            public T Current
            {
                get
                {
                    _owner.CurrentReads++;
                    return _owner._value;
                }
            }

            object IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                _owner.MoveNextCalls++;
                if (_index < _owner._yieldedCount)
                {
                    _index++;
                    return true;
                }
                return false;
            }

            public void Reset() => throw new NotSupportedException();
            public void Dispose() => _owner.DisposeCalls++;
        }
    }
}

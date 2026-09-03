using System.Collections;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

static class QuantityAccumulatorCardinalityModuleSmoke
{
    private const int MaximumEntries = 100_000;

    public static void Run()
    {
        var elementId = ElementId.New();
        var fact = new QuantityFact(elementId, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d));

        var exactLimit = QuantityAccumulator.Summarize(Enumerable.Repeat(fact, MaximumEntries));
        Equal(1, exactLimit.Count);
        Equal(MaximumEntries, exactLimit[0].FactCount);
        Equal(1, exactLimit[0].ElementCount);
        Equal((double)MaximumEntries, exactLimit[0].Quantity.Value);

        Throws<InvalidOperationException>(() => QuantityAccumulator.Summarize(
            new HostileEnumerable<QuantityFact>(fact, MaximumEntries + 1)));
        Throws<InvalidOperationException>(() => QuantityAccumulator.Summarize(
            new CountDriftCollection<QuantityFact>(fact, advertisedCount: 1, yieldedCount: 2)));

        var overrunFacts = new CurrentTrackingCountCollection<QuantityFact>(fact, advertisedCount: 1, yieldedCount: 2);
        Throws<InvalidOperationException>(() => QuantityAccumulator.Summarize(overrunFacts));
        Equal(0, overrunFacts.OverrunCurrentReads);

        Throws<InvalidOperationException>(() => QuantityAccumulator.Summarize(
            new OversizedCountCollection<QuantityFact>(fact, MaximumEntries + 1)));
        Throws<ArgumentException>(() => QuantityAccumulator.Summarize(new QuantityFact[] { null! }));
        Equal(0, QuantityAccumulator.Summarize(Array.Empty<QuantityFact>()).Count);
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

    private sealed class HostileEnumerable<T> : IEnumerable<T>
    {
        private readonly T _value;
        private readonly int _successfulMoves;

        public HostileEnumerable(T value, int successfulMoves)
        {
            _value = value;
            _successfulMoves = successfulMoves;
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(_value, _successfulMoves);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator : IEnumerator<T>
        {
            private readonly T _value;
            private readonly int _successfulMoves;
            private int _moves;

            public Enumerator(T value, int successfulMoves)
            {
                _value = value;
                _successfulMoves = successfulMoves;
            }

            public T Current => _value;
            object IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                if (_moves < _successfulMoves)
                {
                    _moves++;
                    return true;
                }

                throw new HostileEnumerationException();
            }

            public void Reset() => throw new NotSupportedException();
            public void Dispose() { }
        }
    }

    private sealed class CountDriftCollection<T> : ICollection<T>
    {
        private readonly T _value;
        private readonly int _yieldedCount;

        public CountDriftCollection(T value, int advertisedCount, int yieldedCount)
        {
            _value = value;
            Count = advertisedCount;
            _yieldedCount = yieldedCount;
        }

        public int Count { get; }
        public bool IsReadOnly => true;
        public IEnumerator<T> GetEnumerator() => Enumerable.Repeat(_value, _yieldedCount).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Contains(T item) => EqualityComparer<T>.Default.Equals(item, _value);
        public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
        public void Add(T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
    }

    private sealed class CurrentTrackingCountCollection<T> : ICollection<T>
    {
        private readonly T _value;
        private readonly int _yieldedCount;

        public CurrentTrackingCountCollection(T value, int advertisedCount, int yieldedCount)
        {
            _value = value;
            Count = advertisedCount;
            _yieldedCount = yieldedCount;
        }

        public int Count { get; }
        public int OverrunCurrentReads { get; private set; }
        public bool IsReadOnly => true;
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Contains(T item) => EqualityComparer<T>.Default.Equals(item, _value);
        public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
        public void Add(T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();

        private sealed class Enumerator : IEnumerator<T>
        {
            private readonly CurrentTrackingCountCollection<T> _owner;
            private int _index = -1;

            public Enumerator(CurrentTrackingCountCollection<T> owner) => _owner = owner;

            public T Current
            {
                get
                {
                    if (_index >= _owner.Count)
                        _owner.OverrunCurrentReads++;
                    return _owner._value;
                }
            }

            object IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                if (_index + 1 >= _owner._yieldedCount)
                    return false;
                _index++;
                return true;
            }

            public void Reset() => throw new NotSupportedException();
            public void Dispose() { }
        }
    }

    private sealed class OversizedCountCollection<T> : ICollection<T>
    {
        public OversizedCountCollection(T value, int advertisedCount)
        {
            Value = value;
            Count = advertisedCount;
        }

        private T Value { get; }
        public int Count { get; }
        public bool IsReadOnly => true;
        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException("Oversized Count must fail before enumeration.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Contains(T item) => EqualityComparer<T>.Default.Equals(item, Value);
        public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
        public void Add(T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
    }

    private sealed class HostileEnumerationException : Exception { }
}

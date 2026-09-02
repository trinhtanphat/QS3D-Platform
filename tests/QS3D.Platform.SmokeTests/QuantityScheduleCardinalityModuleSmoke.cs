using System.Collections;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

static class QuantityScheduleCardinalityModuleSmoke
{
    private const int MaximumEntries = 100_000;

    public static void Run()
    {
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall");
        var project = new SemanticProject(ProjectId.New(), "Schedule cardinality");
        project.AddFamily(family);
        var element = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W1", family.Id);
        project.AddElement(element);

        var fact = new QuantityFact(element.Id, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d));

        var exactLimit = QuantityScheduleProjector.Project(project, Enumerable.Repeat(fact, MaximumEntries));
        Equal(1, exactLimit.Rows.Count);
        Equal(1, exactLimit.Rows[0].Quantities.Count);
        Equal(MaximumEntries, exactLimit.Rows[0].Quantities[0].FactCount);
        Equal(1, exactLimit.Rows[0].Quantities[0].ElementCount);
        Equal((double)MaximumEntries, exactLimit.Rows[0].Quantities[0].Quantity.Value);

        Throws<InvalidOperationException>(() => QuantityScheduleProjector.Project(
            project,
            new HostileEnumerable<QuantityFact>(fact, MaximumEntries + 1)));
        Throws<InvalidOperationException>(() => QuantityScheduleProjector.Project(
            project,
            new CountDriftCollection<QuantityFact>(fact, advertisedCount: 1, yieldedCount: 2)));
        Throws<InvalidOperationException>(() => QuantityScheduleProjector.Project(
            project,
            new OversizedCountCollection<QuantityFact>(fact, MaximumEntries + 1)));

        var summary = new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 1d, 1, 1);
        Throws<InvalidOperationException>(() => _ = new QuantityScheduleRow(
            element.Id,
            element.Name,
            element.Kind,
            family.Id,
            family.Name,
            null,
            null,
            new HostileEnumerable<QuantitySummary>(summary, MaximumEntries + 1)));

        var row = new QuantityScheduleRow(
            element.Id,
            element.Name,
            element.Kind,
            family.Id,
            family.Name,
            null,
            null,
            new[] { summary });
        Throws<InvalidOperationException>(() => _ = new QuantitySchedule(
            new HostileEnumerable<QuantityScheduleRow>(row, MaximumEntries + 1)));

        var empty = new QuantitySchedule(Array.Empty<QuantityScheduleRow>());
        Equal(0, empty.Rows.Count);
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

    private sealed class OversizedCountCollection<T> : ICollection<T>
    {
        private readonly T _value;

        public OversizedCountCollection(T value, int advertisedCount)
        {
            _value = value;
            Count = advertisedCount;
        }

        public int Count { get; }
        public bool IsReadOnly => true;
        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException("Oversized Count must fail before enumeration.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Contains(T item) => false;
        public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
        public void Add(T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
    }

    private sealed class HostileEnumerationException : Exception { }
}

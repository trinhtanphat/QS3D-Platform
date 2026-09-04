using System.Collections;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

static class QuantityScheduleKnownCountNoOverreadModuleSmoke
{
    public static void Run()
    {
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall");
        var project = new SemanticProject(ProjectId.New(), "Schedule known-count no-overread");
        project.AddFamily(family);
        var element = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W1", family.Id);
        project.AddElement(element);

        var fact = new QuantityFact(element.Id, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d));
        var source = new UnderreportedCountCollection<QuantityFact>(fact, advertisedCount: 1, yieldedCount: 2);

        Throws<InvalidOperationException>(() => QuantityScheduleProjector.Project(project, source));
        Equal(2, source.MoveNextCalls);
        Equal(1, source.CurrentReads);
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

    private sealed class UnderreportedCountCollection<T> : ICollection<T>
    {
        private readonly T _value;
        private readonly int _yieldedCount;

        public UnderreportedCountCollection(T value, int advertisedCount, int yieldedCount)
        {
            _value = value;
            Count = advertisedCount;
            _yieldedCount = yieldedCount;
        }

        public int Count { get; }
        public bool IsReadOnly => true;
        public int MoveNextCalls { get; private set; }
        public int CurrentReads { get; private set; }

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Contains(T item) => EqualityComparer<T>.Default.Equals(item, _value);
        public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
        public void Add(T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();

        private sealed class Enumerator : IEnumerator<T>
        {
            private readonly UnderreportedCountCollection<T> _owner;
            private int _index;

            public Enumerator(UnderreportedCountCollection<T> owner) => _owner = owner;

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
            public void Dispose() { }
        }
    }
}

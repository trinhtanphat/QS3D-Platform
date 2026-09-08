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

        var underreported = new CountControlledCollection<QuantityFact>(fact, advertisedCount: 1, yieldedCount: 2);
        Throws<InvalidOperationException>(() => QuantityScheduleProjector.Project(project, underreported));
        Equal(2, underreported.MoveNextCalls);
        Equal(1, underreported.CurrentReads);
        Equal(1, underreported.DisposeCalls);

        var zeroUnderreported = new CountControlledCollection<QuantityFact>(fact, advertisedCount: 0, yieldedCount: 1);
        Throws<InvalidOperationException>(() => QuantityScheduleProjector.Project(project, zeroUnderreported));
        Equal(1, zeroUnderreported.MoveNextCalls);
        Equal(0, zeroUnderreported.CurrentReads);
        Equal(1, zeroUnderreported.DisposeCalls);

        var earlyEnd = new CountControlledCollection<QuantityFact>(fact, advertisedCount: 2, yieldedCount: 1);
        Throws<InvalidOperationException>(() => QuantityScheduleProjector.Project(project, earlyEnd));
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

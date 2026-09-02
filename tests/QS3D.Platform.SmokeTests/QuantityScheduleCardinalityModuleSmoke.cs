using System.Collections;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

static class QuantityScheduleCardinalityModuleSmoke
{
    public static void Run()
    {
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall");
        var project = new SemanticProject(ProjectId.New(), "Schedule cardinality");
        project.AddFamily(family);
        var element = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W1", family.Id);
        project.AddElement(element);

        var fact = new QuantityFact(element.Id, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d));
        Throws<InvalidOperationException>(() => QuantityScheduleProjector.Project(
            project,
            new HostileEnumerable<QuantityFact>(fact, 100_001)));

        var summary = new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 1d, 1, 1);
        Throws<InvalidOperationException>(() => _ = new QuantityScheduleRow(
            element.Id,
            element.Name,
            element.Kind,
            family.Id,
            family.Name,
            null,
            null,
            new HostileEnumerable<QuantitySummary>(summary, 100_001)));

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
            new HostileEnumerable<QuantityScheduleRow>(row, 100_001)));
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

    private sealed class HostileEnumerationException : Exception { }
}

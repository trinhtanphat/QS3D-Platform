using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleFactGenerationStabilitySmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        SameCountReplacementIsRejected();
        SameCountReorderIsRejected();
        SameCountProvenanceDriftIsRejected();
        StableCountedFactsRemainAccepted();
        CountObservationsStayBoundedDuringReplay();
        StreamingFactsRemainSinglePassCompatible();
        Console.WriteLine("PASS quantity schedule fact generation stability");
    }

    private static void SameCountReplacementIsRejected()
    {
        var project = CreateProject(out var element);
        var first = Fact(element.Id, "WALL.LENGTH", QuantityDimension.Length, 1d);
        var replacement = Fact(element.Id, "WALL.LENGTH", QuantityDimension.Length, 9d);
        ExpectGenerationDrift(
            project,
            new SameCountDriftCollection<QuantityFact>(new[] { first }, new[] { replacement }),
            "same-count schedule fact replacement");
    }

    private static void SameCountReorderIsRejected()
    {
        var project = CreateProject(out var firstElement);
        var familyId = project.Families.Single().Id;
        var secondElement = new SemanticElement(
            new ElementId(Guid.Parse("81000000-0000-0000-0000-000000000004")),
            SemanticElementKind.Wall,
            "Wall B",
            familyId);
        project.AddElement(secondElement);

        var first = Fact(firstElement.Id, "WALL.LENGTH", QuantityDimension.Length, 1d);
        var second = Fact(secondElement.Id, "WALL.LENGTH", QuantityDimension.Length, 2d);
        ExpectGenerationDrift(
            project,
            new SameCountDriftCollection<QuantityFact>(new[] { first, second }, new[] { second, first }),
            "same-count schedule fact reorder");
    }

    private static void SameCountProvenanceDriftIsRejected()
    {
        var project = CreateProject(out var element);
        var acceptedSource = new CadReference(
            new DrawingId(Guid.Parse("81000000-0000-0000-0000-000000000010")),
            new CadHandle("A10"));
        var replacementSource = new CadReference(
            new DrawingId(Guid.Parse("81000000-0000-0000-0000-000000000011")),
            new CadHandle("B11"));
        element.SetSource(acceptedSource);

        var first = new QuantityFact(
            element.Id,
            "WALL.AREA",
            new QuantityValue(QuantityDimension.Area, 5d),
            acceptedSource);
        var replacement = new QuantityFact(
            element.Id,
            "WALL.AREA",
            new QuantityValue(QuantityDimension.Area, 5d),
            replacementSource);

        ExpectGenerationDrift(
            project,
            new SameCountDriftCollection<QuantityFact>(new[] { first }, new[] { replacement }),
            "same-count schedule fact provenance drift");
    }

    private static void StableCountedFactsRemainAccepted()
    {
        var project = CreateProject(out var element);
        var facts = new List<QuantityFact>
        {
            Fact(element.Id, "WALL.LENGTH", QuantityDimension.Length, 1.25d),
            Fact(element.Id, "WALL.LENGTH", QuantityDimension.Length, 2.75d)
        };

        var schedule = QuantityScheduleProjector.Project(project, facts);
        Require(schedule.Rows.Count == 1, "stable counted schedule facts changed row cardinality");
        Require(schedule.Rows[0].Quantities.Count == 1, "stable counted schedule facts changed summary cardinality");
        var summary = schedule.Rows[0].Quantities[0];
        Require(summary.Quantity.Value == 4d, "stable counted schedule facts changed arithmetic");
        Require(summary.FactCount == 2 && summary.ElementCount == 1,
            "stable counted schedule facts changed evidence counts");
    }

    private static void CountObservationsStayBoundedDuringReplay()
    {
        var project = CreateProject(out var element);
        var source = new CountBudgetCollection<QuantityFact>(
            new[]
            {
                Fact(element.Id, "WALL.LENGTH", QuantityDimension.Length, 1d),
                Fact(element.Id, "WALL.LENGTH", QuantityDimension.Length, 2d),
                Fact(element.Id, "WALL.LENGTH", QuantityDimension.Length, 3d),
                Fact(element.Id, "WALL.LENGTH", QuantityDimension.Length, 4d)
            },
            maximumCountReads: 3);

        var schedule = QuantityScheduleProjector.Project(project, source);
        Require(schedule.Rows.Count == 1 && schedule.Rows[0].Quantities[0].Quantity.Value == 10d,
            "bounded Count observation control changed schedule semantics");
        Require(source.CountReads <= 3,
            "schedule fact replay performed unbounded Count observations");
    }

    private static void StreamingFactsRemainSinglePassCompatible()
    {
        var project = CreateProject(out var element);
        var source = new SinglePassEnumerable<QuantityFact>(
            new[] { Fact(element.Id, "WALL.AREA", QuantityDimension.Area, 3d) });

        var schedule = QuantityScheduleProjector.Project(project, source);
        Require(schedule.Rows.Count == 1 && schedule.Rows[0].Quantities[0].Quantity.Value == 3d,
            "streaming schedule facts changed");
        Require(source.EnumerationCount == 1,
            "raw streaming schedule facts were replayed unexpectedly");
    }

    private static void ExpectGenerationDrift(
        SemanticProject project,
        IEnumerable<QuantityFact> source,
        string label)
    {
        try
        {
            _ = QuantityScheduleProjector.Project(project, source);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.IndexOf("content changed during materialization", StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            throw new InvalidOperationException(label + " failed for the wrong reason: " + ex.Message, ex);
        }

        throw new InvalidOperationException(label + " was accepted unexpectedly.");
    }

    private static SemanticProject CreateProject(out SemanticElement element)
    {
        var project = new SemanticProject(
            new ProjectId(Guid.Parse("81000000-0000-0000-0000-000000000001")),
            "Schedule generation stability");
        var familyId = new FamilyId(Guid.Parse("81000000-0000-0000-0000-000000000002"));
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        element = new SemanticElement(
            new ElementId(Guid.Parse("81000000-0000-0000-0000-000000000003")),
            SemanticElementKind.Wall,
            "Wall A",
            familyId);
        project.AddElement(element);
        return project;
    }

    private static QuantityFact Fact(ElementId elementId, string code, QuantityDimension dimension, double value)
        => new(elementId, code, new QuantityValue(dimension, value));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
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

    private sealed class CountBudgetCollection<T> : ICollection<T>
    {
        private readonly T[] _items;
        private readonly int _maximumCountReads;
        private int _countReads;

        internal CountBudgetCollection(T[] items, int maximumCountReads)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _maximumCountReads = maximumCountReads;
        }

        public int Count
        {
            get
            {
                _countReads++;
                if (_countReads > _maximumCountReads)
                    throw new InvalidOperationException("Schedule projection exceeded the Count observation budget.");
                return _items.Length;
            }
        }

        internal int CountReads => _countReads;
        public bool IsReadOnly => true;
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Contains(T item) => ((ICollection<T>)_items).Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public void Add(T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
    }

    private sealed class SinglePassEnumerable<T> : IEnumerable<T>
    {
        private readonly T[] _items;
        private int _enumerationCount;

        internal SinglePassEnumerable(T[] items)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
        }

        internal int EnumerationCount => _enumerationCount;

        public IEnumerator<T> GetEnumerator()
        {
            _enumerationCount++;
            if (_enumerationCount > 1)
                throw new InvalidOperationException("Streaming source was enumerated more than once.");
            return ((IEnumerable<T>)_items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

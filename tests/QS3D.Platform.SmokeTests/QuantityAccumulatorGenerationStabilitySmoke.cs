using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

internal static class QuantityAccumulatorGenerationStabilitySmoke
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        SameCountReplacementIsRejected();
        SameCountReorderIsRejected();
        StableCountedFactsRemainAccepted();
        StreamingFactsRemainSinglePassCompatible();
        Console.WriteLine("PASS quantity accumulator generation stability");
    }

    private static void SameCountReplacementIsRejected()
    {
        var first = ElementId.New();
        var second = ElementId.New();
        var source = new SameCountDriftCollection<QuantityFact>(
            new[]
            {
                Fact(first, "WALL.LENGTH", QuantityDimension.Length, 1d),
                Fact(second, "WALL.LENGTH", QuantityDimension.Length, 2d)
            },
            new[]
            {
                Fact(first, "WALL.LENGTH", QuantityDimension.Length, 1d),
                Fact(second, "WALL.LENGTH", QuantityDimension.Length, 9d)
            });

        ExpectGenerationDrift(source, "same-count quantity fact replacement");
    }

    private static void SameCountReorderIsRejected()
    {
        var first = Fact(ElementId.New(), "WALL.LENGTH", QuantityDimension.Length, 1d);
        var second = Fact(ElementId.New(), "WALL.LENGTH", QuantityDimension.Length, 2d);
        var source = new SameCountDriftCollection<QuantityFact>(
            new[] { first, second },
            new[] { second, first });

        ExpectGenerationDrift(source, "same-count quantity fact reorder");
    }

    private static void StableCountedFactsRemainAccepted()
    {
        var facts = new List<QuantityFact>
        {
            Fact(ElementId.New(), "WALL.LENGTH", QuantityDimension.Length, 1.25d),
            Fact(ElementId.New(), "WALL.LENGTH", QuantityDimension.Length, 2.75d)
        };

        var summaries = QuantityAccumulator.Summarize(facts);
        Require(summaries.Count == 1, "stable counted quantity facts changed summary cardinality");
        Require(summaries[0].Quantity.Value == 4d, "stable counted quantity facts changed arithmetic");
        Require(summaries[0].FactCount == 2 && summaries[0].ElementCount == 2,
            "stable counted quantity facts changed evidence counts");
    }

    private static void StreamingFactsRemainSinglePassCompatible()
    {
        var element = ElementId.New();
        var summaries = QuantityAccumulator.Summarize(Yield(Fact(element, "WALL.AREA", QuantityDimension.Area, 3d)));
        Require(summaries.Count == 1 && summaries[0].Quantity.Value == 3d,
            "streaming quantity facts changed");
    }

    private static void ExpectGenerationDrift(IEnumerable<QuantityFact> source, string label)
    {
        try
        {
            _ = QuantityAccumulator.Summarize(source);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.IndexOf("content changed during materialization", StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            throw new InvalidOperationException(label + " failed for the wrong reason: " + ex.Message, ex);
        }

        throw new InvalidOperationException(label + " was accepted unexpectedly.");
    }

    private static QuantityFact Fact(ElementId elementId, string code, QuantityDimension dimension, double value)
        => new(elementId, code, new QuantityValue(dimension, value));

    private static IEnumerable<QuantityFact> Yield(QuantityFact fact)
    {
        yield return fact;
    }

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
            if (first is null) throw new ArgumentNullException(nameof(first));
            if (second is null) throw new ArgumentNullException(nameof(second));
            if (first.Length != second.Length)
                throw new ArgumentException("Drift generations must preserve Count.");
            _first = first;
            _second = second;
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

using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRuleGenerationStabilityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        RejectFactorReplacement();
        RejectFactorReorder();
        RejectCatalogRuleReplacement();
        PreserveSinglePassStreamingInputs();
        Console.WriteLine("PASS quantity rule generation stability");
    }

    private static void RejectFactorReplacement()
    {
        var source = new SameCountDriftCollection<QuantityFactor>(
            new[] { new QuantityFactor("Length", QuantityUnit.Meter) },
            new[] { new QuantityFactor("Length", QuantityUnit.Centimeter) });

        ExpectContentDrift(
            () => _ = new QuantityRuleDefinition(
                SemanticElementKind.Wall,
                "WALL.LENGTH",
                QuantityDimension.Length,
                source),
            "same-count quantity factor replacement");
    }

    private static void RejectFactorReorder()
    {
        var length = new QuantityFactor("Length", QuantityUnit.Meter);
        var width = new QuantityFactor("Width", QuantityUnit.Meter);
        var source = new SameCountDriftCollection<QuantityFactor>(
            new[] { length, width },
            new[] { width, length });

        ExpectContentDrift(
            () => _ = new QuantityRuleDefinition(
                SemanticElementKind.Wall,
                "WALL.AREA",
                QuantityDimension.Area,
                source),
            "same-count quantity factor reorder");
    }

    private static void RejectCatalogRuleReplacement()
    {
        var first = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "WALL.COUNT",
            QuantityDimension.Count,
            multiplier: 1d);
        var replacement = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "WALL.COUNT",
            QuantityDimension.Count,
            multiplier: 2d);
        var source = new SameCountDriftCollection<QuantityRuleDefinition>(
            new[] { first },
            new[] { replacement });

        ExpectContentDrift(
            () => _ = new QuantityRuleCatalog(source),
            "same-count quantity rule replacement");
    }

    private static void PreserveSinglePassStreamingInputs()
    {
        var factorStream = new SinglePassEnumerable<QuantityFactor>(new[]
        {
            new QuantityFactor("Length", QuantityUnit.Meter)
        });
        var rule = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "WALL.LENGTH",
            QuantityDimension.Length,
            factorStream);
        if (rule.Factors.Count != 1 || factorStream.EnumerationCount != 1)
            throw new InvalidOperationException("raw streaming quantity factors lost single-pass semantics.");

        var ruleStream = new SinglePassEnumerable<QuantityRuleDefinition>(new[] { rule });
        var catalog = new QuantityRuleCatalog(ruleStream);
        if (catalog.Rules.Count != 1 || ruleStream.EnumerationCount != 1)
            throw new InvalidOperationException("raw streaming quantity rules lost single-pass semantics.");
    }

    private static void ExpectContentDrift(Action action, string scenario)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.IndexOf("content changed during materialization", StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            throw new InvalidOperationException(scenario + " failed for the wrong reason: " + ex.Message, ex);
        }

        throw new InvalidOperationException(scenario + " was accepted unexpectedly.");
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

    private sealed class SinglePassEnumerable<T> : IEnumerable<T>
    {
        private readonly T[] _items;

        internal SinglePassEnumerable(T[] items) => _items = items ?? throw new ArgumentNullException(nameof(items));
        internal int EnumerationCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            if (EnumerationCount != 1)
                throw new InvalidOperationException("streaming input was enumerated more than once.");
            return ((IEnumerable<T>)_items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

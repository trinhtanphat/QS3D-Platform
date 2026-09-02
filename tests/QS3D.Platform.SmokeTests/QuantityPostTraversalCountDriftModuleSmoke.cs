using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityPostTraversalCountDriftModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var factor = new QuantityFactor("Count", QuantityUnit.Each);
        ExpectInvalidOperation(() => _ = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "WALL.COUNT",
            QuantityDimension.Count,
            new PostTraversalCountDriftCollection<QuantityFactor>(factor)));

        var rule = new QuantityRuleDefinition(SemanticElementKind.Wall, "WALL.COUNT", QuantityDimension.Count);
        ExpectInvalidOperation(() => _ = new QuantityRuleCatalog(
            new PostTraversalCountDriftCollection<QuantityRuleDefinition>(rule)));

        var summary = new QuantitySummary("WALL.LENGTH", QuantityDimension.Length, 1d, 1, 1);
        var elementId = ElementId.New();
        var familyId = FamilyId.New();
        ExpectInvalidOperation(() => _ = new QuantityScheduleRow(
            elementId,
            "W1",
            SemanticElementKind.Wall,
            familyId,
            "Wall",
            null,
            null,
            new PostTraversalCountDriftCollection<QuantitySummary>(summary)));

        var row = new QuantityScheduleRow(
            elementId,
            "W1",
            SemanticElementKind.Wall,
            familyId,
            "Wall",
            null,
            null,
            new[] { summary });
        ExpectInvalidOperation(() => _ = new QuantitySchedule(
            new PostTraversalCountDriftCollection<QuantityScheduleRow>(row)));

        Console.WriteLine("PASS quantity post-traversal Count drift safety");
    }

    private static void ExpectInvalidOperation(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Post-traversal Count drift must fail closed.");
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
        public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
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
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
    }
}

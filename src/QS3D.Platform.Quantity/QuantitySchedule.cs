using System.Collections;
using QS3D.Platform.Domain;

namespace QS3D.Platform.Quantity;

public sealed class QuantityScheduleRow
{
    public QuantityScheduleRow(
        ElementId elementId,
        string elementName,
        SemanticElementKind elementKind,
        FamilyId familyId,
        string familyName,
        FloorId? floorId,
        ZoneId? zoneId,
        IEnumerable<QuantitySummary> quantities)
    {
        if (elementId.Value == Guid.Empty) throw new ArgumentException("Element ID must not be empty.", nameof(elementId));
        if (elementKind == SemanticElementKind.Unknown || !Enum.IsDefined(typeof(SemanticElementKind), elementKind)) throw new ArgumentOutOfRangeException(nameof(elementKind));
        if (familyId.Value == Guid.Empty) throw new ArgumentException("Family ID must not be empty.", nameof(familyId));
        if (floorId.HasValue && floorId.Value.Value == Guid.Empty) throw new ArgumentException("Floor ID must not be empty when supplied.", nameof(floorId));
        if (zoneId.HasValue && zoneId.Value.Value == Guid.Empty) throw new ArgumentException("Zone ID must not be empty when supplied.", nameof(zoneId));
        if (string.IsNullOrWhiteSpace(elementName)) throw new ArgumentException("Element name must not be blank.", nameof(elementName));
        if (string.IsNullOrWhiteSpace(familyName)) throw new ArgumentException("Family name must not be blank.", nameof(familyName));
        if (quantities is null) throw new ArgumentNullException(nameof(quantities));
        var copiedQuantities = QuantityScheduleMaterializer.Materialize(quantities, nameof(quantities), "schedule quantities");
        if (copiedQuantities.Any(static quantity => quantity is null)) throw new ArgumentException("Schedule quantities must not contain null entries.", nameof(quantities));
        EnsureUniqueQuantityKeys(copiedQuantities);
        ElementId = elementId;
        ElementName = elementName.Trim();
        ElementKind = elementKind;
        FamilyId = familyId;
        FamilyName = familyName.Trim();
        FloorId = floorId;
        ZoneId = zoneId;
        var orderedQuantities = copiedQuantities.OrderBy(static quantity => quantity.Code, StringComparer.Ordinal)
            .ThenBy(static quantity => quantity.Quantity.Dimension)
            .ToArray();
        Quantities = Array.AsReadOnly(orderedQuantities);
    }

    public ElementId ElementId { get; }
    public string ElementName { get; }
    public SemanticElementKind ElementKind { get; }
    public FamilyId FamilyId { get; }
    public string FamilyName { get; }
    public FloorId? FloorId { get; }
    public ZoneId? ZoneId { get; }
    public IReadOnlyList<QuantitySummary> Quantities { get; }

    private static void EnsureUniqueQuantityKeys(IEnumerable<QuantitySummary> quantities)
    {
        var dimensionsByCode = new Dictionary<string, HashSet<QuantityDimension>>(StringComparer.Ordinal);
        foreach (var quantity in quantities)
        {
            if (!dimensionsByCode.TryGetValue(quantity.Code, out var dimensions))
            {
                dimensions = new HashSet<QuantityDimension>();
                dimensionsByCode.Add(quantity.Code, dimensions);
            }

            if (!dimensions.Add(quantity.Quantity.Dimension))
                throw new InvalidOperationException($"Duplicate quantity summary for '{quantity.Code}'/{quantity.Quantity.Dimension} in schedule row.");
        }
    }
}

public sealed class QuantitySchedule
{
    public QuantitySchedule(IEnumerable<QuantityScheduleRow> rows)
    {
        if (rows is null) throw new ArgumentNullException(nameof(rows));
        var copiedRows = QuantityScheduleMaterializer.Materialize(rows, nameof(rows), "schedule rows");
        if (copiedRows.Any(static row => row is null)) throw new ArgumentException("Schedule rows must not contain null entries.", nameof(rows));
        var elementIds = new HashSet<ElementId>();
        foreach (var row in copiedRows)
        {
            if (!elementIds.Add(row.ElementId))
                throw new InvalidOperationException($"Duplicate schedule element {row.ElementId.Value:D}.");
        }
        var orderedRows = copiedRows.OrderBy(static row => row.ElementKind)
            .ThenBy(static row => row.ElementName, StringComparer.Ordinal)
            .ThenBy(static row => row.ElementId.Value)
            .ToArray();
        Rows = Array.AsReadOnly(orderedRows);
    }

    public IReadOnlyList<QuantityScheduleRow> Rows { get; }
}

public static class QuantityScheduleProjector
{
    public static QuantitySchedule Project(
        SemanticProject project,
        IEnumerable<QuantityFact> facts,
        bool includeElementsWithoutQuantities = false)
    {
        if (project is null) throw new ArgumentNullException(nameof(project));
        if (facts is null) throw new ArgumentNullException(nameof(facts));
        if (includeElementsWithoutQuantities && project.Elements.Count > QuantityScheduleMaterializer.MaximumEntries)
            throw new InvalidOperationException($"Schedule rows exceed the supported maximum of {QuantityScheduleMaterializer.MaximumEntries} entries.");

        var elements = project.Elements.ToDictionary(static element => element.Id);
        var copiedFacts = QuantityScheduleMaterializer.Materialize(facts, nameof(facts), "quantity facts");
        if (copiedFacts.Any(static fact => fact is null)) throw new ArgumentException("Quantity facts must not contain null entries.", nameof(facts));
        var factsByElement = new Dictionary<ElementId, List<QuantityFact>>();
        foreach (var fact in copiedFacts)
        {
            if (!elements.TryGetValue(fact.ElementId, out var element))
                throw new InvalidOperationException($"Quantity fact '{fact.Code}' references element {fact.ElementId.Value:D}, which is not in the project.");
            if (fact.SourceReference != element.SourceReference)
                throw new InvalidOperationException($"Quantity fact '{fact.Code}' source provenance does not match element {fact.ElementId.Value:D}.");
            if (!factsByElement.TryGetValue(fact.ElementId, out var bucket))
            {
                bucket = new List<QuantityFact>();
                factsByElement.Add(fact.ElementId, bucket);
            }
            bucket.Add(fact);
        }

        var rows = new List<QuantityScheduleRow>();
        foreach (var element in project.Elements)
        {
            factsByElement.TryGetValue(element.Id, out var elementFacts);
            if (!includeElementsWithoutQuantities && (elementFacts is null || elementFacts.Count == 0)) continue;
            if (!project.TryGetFamily(element.FamilyId, out var family) || family is null)
                throw new InvalidOperationException($"Element '{element.Name}' references a missing family.");
            if (element.FloorId.HasValue && !project.ContainsFloor(element.FloorId.Value))
                throw new InvalidOperationException($"Element '{element.Name}' references a floor outside the project.");
            if (element.ZoneId.HasValue && !project.ContainsZone(element.ZoneId.Value))
                throw new InvalidOperationException($"Element '{element.Name}' references a zone outside the project.");
            var summaries = elementFacts is null
                ? Array.Empty<QuantitySummary>()
                : QuantityAccumulator.Summarize(elementFacts).ToArray();
            rows.Add(new QuantityScheduleRow(
                element.Id,
                element.Name,
                element.Kind,
                element.FamilyId,
                family.Name,
                element.FloorId,
                element.ZoneId,
                summaries));
        }

        return new QuantitySchedule(rows);
    }
}

internal static class QuantityScheduleMaterializer
{
    internal const int MaximumEntries = 100_000;

    internal static T[] Materialize<T>(IEnumerable<T> source, string parameterName, string entryDescription)
    {
        if (source is null) throw new ArgumentNullException(parameterName);
        if (string.IsNullOrWhiteSpace(entryDescription)) throw new ArgumentException("Entry description must not be blank.", nameof(entryDescription));

        int? advertisedCount = null;
        CaptureCount(source as ICollection<T>, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);
        CaptureCount(source as IReadOnlyCollection<T>, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);
        CaptureCount(source as ICollection, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);

        var result = advertisedCount.HasValue ? new List<T>(advertisedCount.Value) : new List<T>();
        foreach (var item in source)
        {
            if (result.Count >= MaximumEntries)
                throw new InvalidOperationException($"{entryDescription} exceed the supported maximum of {MaximumEntries} entries.");
            result.Add(item);
        }

        if (advertisedCount.HasValue && advertisedCount.Value != result.Count)
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");

        int? finalCount = null;
        CaptureCount(source as ICollection<T>, static collection => collection.Count, ref finalCount, parameterName, entryDescription);
        CaptureCount(source as IReadOnlyCollection<T>, static collection => collection.Count, ref finalCount, parameterName, entryDescription);
        CaptureCount(source as ICollection, static collection => collection.Count, ref finalCount, parameterName, entryDescription);
        if (advertisedCount.HasValue != finalCount.HasValue
            || (advertisedCount.HasValue && advertisedCount.Value != finalCount!.Value))
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");

        return result.ToArray();
    }

    private static void CaptureCount<TCollection>(
        TCollection? collection,
        Func<TCollection, int> getCount,
        ref int? advertisedCount,
        string parameterName,
        string entryDescription)
        where TCollection : class
    {
        if (collection is null) return;
        var count = getCount(collection);
        if (count < 0)
            throw new ArgumentException($"{entryDescription} reported a negative Count.", parameterName);
        if (count > MaximumEntries)
            throw new InvalidOperationException($"{entryDescription} exceed the supported maximum of {MaximumEntries} entries.");
        if (advertisedCount.HasValue && advertisedCount.Value != count)
            throw new InvalidOperationException($"{entryDescription} expose conflicting Count values.");
        advertisedCount = count;
    }
}

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
        : this(elementId, elementName, elementKind, familyId, familyName, floorId, zoneId, quantities, null)
    {
    }

    public QuantityScheduleRow(
        ElementId elementId,
        string elementName,
        SemanticElementKind elementKind,
        FamilyId familyId,
        string familyName,
        FloorId? floorId,
        ZoneId? zoneId,
        IEnumerable<QuantitySummary> quantities,
        CadReference? sourceReference)
    {
        if (elementId.Value == Guid.Empty) throw new ArgumentException("Element ID must not be empty.", nameof(elementId));
        if (elementKind == SemanticElementKind.Unknown || !Enum.IsDefined(typeof(SemanticElementKind), elementKind)) throw new ArgumentOutOfRangeException(nameof(elementKind));
        if (familyId.Value == Guid.Empty) throw new ArgumentException("Family ID must not be empty.", nameof(familyId));
        if (floorId.HasValue && floorId.Value.Value == Guid.Empty) throw new ArgumentException("Floor ID must not be empty when supplied.", nameof(floorId));
        if (zoneId.HasValue && zoneId.Value.Value == Guid.Empty) throw new ArgumentException("Zone ID must not be empty when supplied.", nameof(zoneId));
        if (string.IsNullOrWhiteSpace(elementName)) throw new ArgumentException("Element name must not be blank.", nameof(elementName));
        if (string.IsNullOrWhiteSpace(familyName)) throw new ArgumentException("Family name must not be blank.", nameof(familyName));
        if (sourceReference.HasValue) ValidateSourceReference(sourceReference.Value, nameof(sourceReference));
        if (quantities is null) throw new ArgumentNullException(nameof(quantities));
        var copiedQuantities = QuantityScheduleMaterializer.MaterializeStableQuantitySummaries(quantities, nameof(quantities), "schedule quantities");
        if (copiedQuantities.Any(static quantity => quantity is null)) throw new ArgumentException("Schedule quantities must not contain null entries.", nameof(quantities));
        EnsureRowLocalSummaryAffinity(copiedQuantities);
        EnsureUniqueQuantityKeys(copiedQuantities);
        ElementId = elementId;
        ElementName = elementName.Trim();
        ElementKind = elementKind;
        FamilyId = familyId;
        FamilyName = familyName.Trim();
        FloorId = floorId;
        ZoneId = zoneId;
        SourceReference = sourceReference;
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
    public CadReference? SourceReference { get; }
    public IReadOnlyList<QuantitySummary> Quantities { get; }

    private static void ValidateSourceReference(CadReference sourceReference, string parameterName)
    {
        if (sourceReference.DrawingId.Value == Guid.Empty)
            throw new ArgumentException("Source drawing ID must not be empty when supplied.", parameterName);
        if (string.IsNullOrWhiteSpace(sourceReference.Handle.Value))
            throw new ArgumentException("Source CAD handle must not be empty when supplied.", parameterName);
    }

    private static void EnsureRowLocalSummaryAffinity(IEnumerable<QuantitySummary> quantities)
    {
        foreach (var quantity in quantities)
        {
            if (quantity.FactCount == 0 || quantity.ElementCount != 1)
                throw new InvalidOperationException($"Schedule row quantity '{quantity.Code}'/{quantity.Quantity.Dimension} must be backed by facts from exactly one element.");
        }
    }

    private static void EnsureUniqueQuantityKeys(IEnumerable<QuantitySummary> quantities)
    {
        var dimensionByCode = new Dictionary<string, QuantityDimension>(StringComparer.Ordinal);
        foreach (var quantity in quantities)
        {
            if (!dimensionByCode.TryAdd(quantity.Code, quantity.Quantity.Dimension))
            {
                var existingDimension = dimensionByCode[quantity.Code];
                if (existingDimension != quantity.Quantity.Dimension)
                    throw new InvalidOperationException($"Quantity code '{quantity.Code}' is declared with both {existingDimension} and {quantity.Quantity.Dimension} dimensions in schedule row.");
                throw new InvalidOperationException($"Duplicate quantity summary for '{quantity.Code}'/{quantity.Quantity.Dimension} in schedule row.");
            }
        }
    }
}

public sealed class QuantitySchedule
{
    public QuantitySchedule(IEnumerable<QuantityScheduleRow> rows)
    {
        if (rows is null) throw new ArgumentNullException(nameof(rows));
        var copiedRows = QuantityScheduleMaterializer.MaterializeStableScheduleRows(rows, nameof(rows), "schedule rows");
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

        // In include-empty mode every project element necessarily becomes one output row. Reject
        // impossible cardinality before allocating any project snapshot state. Keep the later
        // snapshot-length check as a fail-closed guard if project membership changes concurrently
        // between this admission read and snapshot materialization.
        if (includeElementsWithoutQuantities && project.Elements.Count > QuantityScheduleMaterializer.MaximumEntries)
            throw new InvalidOperationException($"Schedule rows exceed the supported maximum of {QuantityScheduleMaterializer.MaximumEntries} entries.");

        // Snapshot every project value that this projection can observe before executing the
        // caller-controlled facts enumerable. Keeping SemanticElement references here would
        // still permit SetSource/SetLocation (or project membership changes) to alter one
        // in-flight schedule after admission.
        var familyNames = project.Families.ToDictionary(static family => family.Id, static family => family.Name);
        var floorIds = new HashSet<FloorId>(project.Floors.Select(static floor => floor.Id));
        var zoneIds = new HashSet<ZoneId>(project.Zones.Select(static zone => zone.Id));
        var elementSnapshots = project.Elements
            .Select(static element => new ElementProjectionSnapshot(
                element.Id,
                element.Name,
                element.Kind,
                element.FamilyId,
                element.FloorId,
                element.ZoneId,
                element.SourceReference))
            .ToArray();

        if (includeElementsWithoutQuantities && elementSnapshots.Length > QuantityScheduleMaterializer.MaximumEntries)
            throw new InvalidOperationException($"Schedule rows exceed the supported maximum of {QuantityScheduleMaterializer.MaximumEntries} entries.");

        var elements = elementSnapshots.ToDictionary(static element => element.Id);
        var copiedFacts = QuantityScheduleMaterializer.MaterializeStableQuantityFacts(facts, nameof(facts), "quantity facts");
        if (copiedFacts.Any(static fact => fact is null)) throw new ArgumentException("Quantity facts must not contain null entries.", nameof(facts));
        var factsByElement = new Dictionary<ElementId, List<QuantityFact>>();
        foreach (var fact in copiedFacts)
        {
            if (!elements.TryGetValue(fact.ElementId, out var element))
                throw new InvalidOperationException($"Quantity fact '{fact.Code}' references element {fact.ElementId.Value:D}, which is not in the project snapshot.");
            if (fact.SourceReference != element.SourceReference)
                throw new InvalidOperationException($"Quantity fact '{fact.Code}' source provenance does not match element {fact.ElementId.Value:D} in the project snapshot.");
            if (!factsByElement.TryGetValue(fact.ElementId, out var bucket))
            {
                bucket = new List<QuantityFact>();
                factsByElement.Add(fact.ElementId, bucket);
            }
            bucket.Add(fact);
        }

        var rows = new List<QuantityScheduleRow>();
        foreach (var element in elementSnapshots)
        {
            factsByElement.TryGetValue(element.Id, out var elementFacts);
            if (!includeElementsWithoutQuantities && (elementFacts is null || elementFacts.Count == 0)) continue;
            if (!familyNames.TryGetValue(element.FamilyId, out var familyName))
                throw new InvalidOperationException($"Element '{element.Name}' references a missing family in the project snapshot.");
            if (element.FloorId.HasValue && !floorIds.Contains(element.FloorId.Value))
                throw new InvalidOperationException($"Element '{element.Name}' references a floor outside the project snapshot.");
            if (element.ZoneId.HasValue && !zoneIds.Contains(element.ZoneId.Value))
                throw new InvalidOperationException($"Element '{element.Name}' references a zone outside the project snapshot.");
            var summaries = elementFacts is null
                ? Array.Empty<QuantitySummary>()
                : QuantityAccumulator.Summarize(elementFacts).ToArray();
            rows.Add(new QuantityScheduleRow(
                element.Id,
                element.Name,
                element.Kind,
                element.FamilyId,
                familyName,
                element.FloorId,
                element.ZoneId,
                summaries,
                element.SourceReference));
        }

        return new QuantitySchedule(rows);
    }

    private readonly struct ElementProjectionSnapshot
    {
        public ElementProjectionSnapshot(
            ElementId id,
            string name,
            SemanticElementKind kind,
            FamilyId familyId,
            FloorId? floorId,
            ZoneId? zoneId,
            CadReference? sourceReference)
        {
            Id = id;
            Name = name;
            Kind = kind;
            FamilyId = familyId;
            FloorId = floorId;
            ZoneId = zoneId;
            SourceReference = sourceReference;
        }

        public ElementId Id { get; }
        public string Name { get; }
        public SemanticElementKind Kind { get; }
        public FamilyId FamilyId { get; }
        public FloorId? FloorId { get; }
        public ZoneId? ZoneId { get; }
        public CadReference? SourceReference { get; }
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

    internal static QuantityScheduleRow[] MaterializeStableScheduleRows(
        IEnumerable<QuantityScheduleRow> source,
        string parameterName,
        string entryDescription)
    {
        if (source is null) throw new ArgumentNullException(parameterName);
        if (string.IsNullOrWhiteSpace(entryDescription)) throw new ArgumentException("Entry description must not be blank.", nameof(entryDescription));

        int? advertisedCount = null;
        CaptureCount(source as ICollection<QuantityScheduleRow>, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);
        CaptureCount(source as IReadOnlyCollection<QuantityScheduleRow>, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);
        CaptureCount(source as ICollection, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);

        var result = advertisedCount.HasValue ? new List<QuantityScheduleRow>(advertisedCount.Value) : new List<QuantityScheduleRow>();
        foreach (var row in source)
        {
            if (result.Count >= MaximumEntries)
                throw new InvalidOperationException($"{entryDescription} exceed the supported maximum of {MaximumEntries} entries.");
            result.Add(row);
        }

        if (advertisedCount.HasValue && advertisedCount.Value != result.Count)
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");

        RequireStableKnownRowCount(source, advertisedCount, result.Count, parameterName, entryDescription);
        if (!advertisedCount.HasValue)
            return result.ToArray();

        var snapshot = result.ToArray();
        var index = 0;
        using (var enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                if (index >= snapshot.Length || !QuantityScheduleRowStateEquals(snapshot[index], enumerator.Current))
                    throw new InvalidOperationException($"{entryDescription} content changed during materialization.");
                index++;
            }
        }

        if (index != snapshot.Length)
            throw new InvalidOperationException($"{entryDescription} content changed during materialization.");
        RequireStableKnownRowCount(source, advertisedCount, snapshot.Length, parameterName, entryDescription);
        return snapshot;
    }

    internal static QuantitySummary[] MaterializeStableQuantitySummaries(
        IEnumerable<QuantitySummary> source,
        string parameterName,
        string entryDescription)
    {
        if (source is null) throw new ArgumentNullException(parameterName);
        if (string.IsNullOrWhiteSpace(entryDescription)) throw new ArgumentException("Entry description must not be blank.", nameof(entryDescription));

        int? advertisedCount = null;
        CaptureCount(source as ICollection<QuantitySummary>, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);
        CaptureCount(source as IReadOnlyCollection<QuantitySummary>, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);
        CaptureCount(source as ICollection, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);

        var result = advertisedCount.HasValue ? new List<QuantitySummary>(advertisedCount.Value) : new List<QuantitySummary>();
        foreach (var summary in source)
        {
            if (result.Count >= MaximumEntries)
                throw new InvalidOperationException($"{entryDescription} exceed the supported maximum of {MaximumEntries} entries.");
            result.Add(summary);
        }

        if (advertisedCount.HasValue && advertisedCount.Value != result.Count)
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");

        RequireStableKnownSummaryCount(source, advertisedCount, result.Count, parameterName, entryDescription);
        if (!advertisedCount.HasValue)
            return result.ToArray();

        var snapshot = result.ToArray();
        var index = 0;
        using (var enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                if (index >= snapshot.Length || !QuantitySummaryStateEquals(snapshot[index], enumerator.Current))
                    throw new InvalidOperationException($"{entryDescription} content changed during materialization.");
                index++;
            }
        }

        if (index != snapshot.Length)
            throw new InvalidOperationException($"{entryDescription} content changed during materialization.");
        RequireStableKnownSummaryCount(source, advertisedCount, snapshot.Length, parameterName, entryDescription);
        return snapshot;
    }

    internal static QuantityFact[] MaterializeStableQuantityFacts(
        IEnumerable<QuantityFact> source,
        string parameterName,
        string entryDescription)
    {
        if (source is null) throw new ArgumentNullException(parameterName);
        if (string.IsNullOrWhiteSpace(entryDescription)) throw new ArgumentException("Entry description must not be blank.", nameof(entryDescription));

        int? advertisedCount = null;
        CaptureCount(source as ICollection<QuantityFact>, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);
        CaptureCount(source as IReadOnlyCollection<QuantityFact>, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);
        CaptureCount(source as ICollection, static collection => collection.Count, ref advertisedCount, parameterName, entryDescription);

        var result = advertisedCount.HasValue ? new List<QuantityFact>(advertisedCount.Value) : new List<QuantityFact>();
        foreach (var fact in source)
        {
            if (result.Count >= MaximumEntries)
                throw new InvalidOperationException($"{entryDescription} exceed the supported maximum of {MaximumEntries} entries.");
            result.Add(fact);
        }

        if (advertisedCount.HasValue && advertisedCount.Value != result.Count)
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");

        RequireStableKnownCount(source, advertisedCount, result.Count, parameterName, entryDescription);
        if (!advertisedCount.HasValue)
            return result.ToArray();

        var snapshot = result.ToArray();
        var index = 0;
        using (var enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                if (index >= snapshot.Length || !QuantityFactStateEquals(snapshot[index], enumerator.Current))
                    throw new InvalidOperationException($"{entryDescription} content changed during materialization.");
                index++;
            }
        }

        if (index != snapshot.Length)
            throw new InvalidOperationException($"{entryDescription} content changed during materialization.");
        RequireStableKnownCount(source, advertisedCount, snapshot.Length, parameterName, entryDescription);
        return snapshot;
    }

    private static bool QuantityScheduleRowStateEquals(QuantityScheduleRow? left, QuantityScheduleRow? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        if (!left.ElementId.Equals(right.ElementId)
            || !StringComparer.Ordinal.Equals(left.ElementName, right.ElementName)
            || left.ElementKind != right.ElementKind
            || !left.FamilyId.Equals(right.FamilyId)
            || !StringComparer.Ordinal.Equals(left.FamilyName, right.FamilyName)
            || !Nullable.Equals(left.FloorId, right.FloorId)
            || !Nullable.Equals(left.ZoneId, right.ZoneId)
            || !Nullable.Equals(left.SourceReference, right.SourceReference)
            || left.Quantities.Count != right.Quantities.Count)
            return false;

        for (var index = 0; index < left.Quantities.Count; index++)
        {
            if (!QuantitySummaryStateEquals(left.Quantities[index], right.Quantities[index]))
                return false;
        }
        return true;
    }

    private static bool QuantitySummaryStateEquals(QuantitySummary? left, QuantitySummary? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        return StringComparer.Ordinal.Equals(left.Code, right.Code)
            && left.Quantity.Equals(right.Quantity)
            && left.FactCount == right.FactCount
            && left.ElementCount == right.ElementCount;
    }

    private static bool QuantityFactStateEquals(QuantityFact? left, QuantityFact? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        return left.ElementId.Equals(right.ElementId)
            && StringComparer.Ordinal.Equals(left.Code, right.Code)
            && left.Quantity.Equals(right.Quantity)
            && Nullable.Equals(left.SourceReference, right.SourceReference);
    }

    private static void RequireStableKnownRowCount(
        IEnumerable<QuantityScheduleRow> source,
        int? advertisedCount,
        int materializedCount,
        string parameterName,
        string entryDescription)
    {
        int? currentCount = null;
        CaptureCount(source as ICollection<QuantityScheduleRow>, static collection => collection.Count, ref currentCount, parameterName, entryDescription);
        CaptureCount(source as IReadOnlyCollection<QuantityScheduleRow>, static collection => collection.Count, ref currentCount, parameterName, entryDescription);
        CaptureCount(source as ICollection, static collection => collection.Count, ref currentCount, parameterName, entryDescription);

        if (currentCount.HasValue && currentCount.Value != materializedCount)
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");
        if (advertisedCount.HasValue != currentCount.HasValue
            || (advertisedCount.HasValue && advertisedCount.Value != currentCount!.Value))
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");
    }

    private static void RequireStableKnownSummaryCount(
        IEnumerable<QuantitySummary> source,
        int? advertisedCount,
        int materializedCount,
        string parameterName,
        string entryDescription)
    {
        int? currentCount = null;
        CaptureCount(source as ICollection<QuantitySummary>, static collection => collection.Count, ref currentCount, parameterName, entryDescription);
        CaptureCount(source as IReadOnlyCollection<QuantitySummary>, static collection => collection.Count, ref currentCount, parameterName, entryDescription);
        CaptureCount(source as ICollection, static collection => collection.Count, ref currentCount, parameterName, entryDescription);

        if (currentCount.HasValue && currentCount.Value != materializedCount)
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");
        if (advertisedCount.HasValue != currentCount.HasValue
            || (advertisedCount.HasValue && advertisedCount.Value != currentCount!.Value))
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");
    }

    private static void RequireStableKnownCount(
        IEnumerable<QuantityFact> source,
        int? advertisedCount,
        int materializedCount,
        string parameterName,
        string entryDescription)
    {
        int? currentCount = null;
        CaptureCount(source as ICollection<QuantityFact>, static collection => collection.Count, ref currentCount, parameterName, entryDescription);
        CaptureCount(source as IReadOnlyCollection<QuantityFact>, static collection => collection.Count, ref currentCount, parameterName, entryDescription);
        CaptureCount(source as ICollection, static collection => collection.Count, ref currentCount, parameterName, entryDescription);

        if (currentCount.HasValue && currentCount.Value != materializedCount)
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");
        if (advertisedCount.HasValue != currentCount.HasValue
            || (advertisedCount.HasValue && advertisedCount.Value != currentCount!.Value))
            throw new InvalidOperationException($"{entryDescription} changed cardinality during materialization.");
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

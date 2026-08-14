using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleInvariantModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var elementId = ElementId.New();
        var familyId = FamilyId.New();
        var quantity = new QuantitySummary("COUNT", QuantityDimension.Count, 1d, 1, 1);
        var invalidKind = (SemanticElementKind)int.MaxValue;

        Throws<ArgumentOutOfRangeException>(() => new QuantityScheduleRow(
            elementId, "Element", invalidKind, familyId, "Family", null, null, new[] { quantity }));
        Throws<ArgumentException>(() => new QuantityScheduleRow(
            elementId, "Element", SemanticElementKind.Wall, familyId, "Family", new FloorId(Guid.Empty), null, new[] { quantity }));
        Throws<ArgumentException>(() => new QuantityScheduleRow(
            elementId, "Element", SemanticElementKind.Wall, familyId, "Family", null, new ZoneId(Guid.Empty), new[] { quantity }));
        Throws<ArgumentException>(() => new QuantityScheduleRow(
            elementId, "Element", SemanticElementKind.Wall, familyId, "Family", null, null, new QuantitySummary[] { null! }));
        Throws<ArgumentException>(() => new QuantitySchedule(new QuantityScheduleRow[] { null! }));

        var valid = new QuantityScheduleRow(
            elementId, "Element", SemanticElementKind.Wall, familyId, "Family", null, null, new[] { quantity });
        var schedule = new QuantitySchedule(new[] { valid });
        if (schedule.Rows.Count != 1 || schedule.Rows[0] != valid)
            throw new InvalidOperationException("Valid schedule row was not retained.");

        Console.WriteLine("PASS quantity schedule structural invariants");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}

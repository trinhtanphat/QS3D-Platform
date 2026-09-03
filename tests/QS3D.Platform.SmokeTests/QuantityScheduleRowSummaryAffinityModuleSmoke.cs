using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleRowSummaryAffinityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var elementId = ElementId.New();
        var familyId = FamilyId.New();

        ExpectRejected("aggregate summary from multiple elements", () => CreateRow(
            elementId,
            familyId,
            new QuantitySummary("VOL", QuantityDimension.Volume, 12d, factCount: 2, elementCount: 2)));

        ExpectRejected("zero-fact summary inside an element row", () => CreateRow(
            elementId,
            familyId,
            new QuantitySummary("VOL", QuantityDimension.Volume, 0d, factCount: 0, elementCount: 0)));

        var zeroValuedFactBacked = CreateRow(
            elementId,
            familyId,
            new QuantitySummary("VOL", QuantityDimension.Volume, 0d, factCount: 2, elementCount: 1));
        Equal(1, zeroValuedFactBacked.Quantities.Count);
        Equal(1, zeroValuedFactBacked.Quantities[0].ElementCount);
        Equal(2, zeroValuedFactBacked.Quantities[0].FactCount);

        var empty = new QuantityScheduleRow(
            elementId,
            "Wall",
            SemanticElementKind.Wall,
            familyId,
            "Wall Family",
            null,
            null,
            Array.Empty<QuantitySummary>());
        Equal(0, empty.Quantities.Count);

        Console.WriteLine("PASS quantity schedule row summary affinity");
    }

    private static QuantityScheduleRow CreateRow(ElementId elementId, FamilyId familyId, QuantitySummary summary) =>
        new QuantityScheduleRow(
            elementId,
            "Wall",
            SemanticElementKind.Wall,
            familyId,
            "Wall Family",
            null,
            null,
            new[] { summary });

    private static void ExpectRejected(string scenario, Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException(scenario + " was accepted.");
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }
}

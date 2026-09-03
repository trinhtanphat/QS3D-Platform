using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleCsvOutputCardinalityModuleSmoke
{
    private const int MaximumRecords = 100_000;

    [ModuleInitializer]
    internal static void Run()
    {
        var summaries = Enumerable.Range(0, MaximumRecords)
            .Select(static index => new QuantitySummary(
                "Q" + index.ToString("D6", CultureInfo.InvariantCulture),
                QuantityDimension.Count,
                1d,
                factCount: 1,
                elementCount: 1))
            .ToArray();

        var exactBoundaryRow = CreateRow(
            new ElementId(Guid.Parse("00000000-0000-0000-0000-000000000001")),
            "Exact Boundary",
            summaries);
        var exactBoundaryCsv = QuantityScheduleCsv.Write(new QuantitySchedule(new[] { exactBoundaryRow }));
        Equal(MaximumRecords + 1, CountCrLf(exactBoundaryCsv)); // header + 100,000 data records

        var extraEmptyRow = CreateRow(
            new ElementId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            "One Too Many",
            Array.Empty<QuantitySummary>());
        Throws<InvalidOperationException>(() => QuantityScheduleCsv.Write(
            new QuantitySchedule(new[] { exactBoundaryRow, extraEmptyRow })));

        Console.WriteLine("PASS quantity schedule CSV output cardinality");
    }

    private static QuantityScheduleRow CreateRow(
        ElementId elementId,
        string name,
        IEnumerable<QuantitySummary> summaries) =>
        new QuantityScheduleRow(
            elementId,
            name,
            SemanticElementKind.Wall,
            new FamilyId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            "Wall Family",
            null,
            null,
            summaries);

    private static int CountCrLf(string value)
    {
        var count = 0;
        for (var index = 0; index + 1 < value.Length; index++)
        {
            if (value[index] == '\r' && value[index + 1] == '\n')
            {
                count++;
                index++;
            }
        }
        return count;
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }
}

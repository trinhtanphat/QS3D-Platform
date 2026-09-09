using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleCsvOutputBudgetModuleSmoke
{
    private const int RepeatedSummaryCount = 600;
    private const int LargeElementNameChars = 30_000;

    [ModuleInitializer]
    internal static void Run()
    {
        RejectsCumulativeCsvAmplification();
        Console.WriteLine("PASS quantity schedule CSV output budget");
    }

    private static void RejectsCumulativeCsvAmplification()
    {
        var summaries = Enumerable.Range(0, RepeatedSummaryCount)
            .Select(static index => new QuantitySummary(
                "Q" + index.ToString("D4", CultureInfo.InvariantCulture),
                QuantityDimension.Count,
                1d,
                factCount: 1,
                elementCount: 1))
            .ToArray();

        var row = new QuantityScheduleRow(
            new ElementId(Guid.Parse("00000000-0000-0000-0000-00000000b001")),
            new string('X', LargeElementNameChars),
            SemanticElementKind.Wall,
            new FamilyId(Guid.Parse("11111111-1111-1111-1111-11111111b001")),
            "Budget Family",
            null,
            null,
            summaries);

        Throws<InvalidOperationException>(() => QuantityScheduleCsv.Write(new QuantitySchedule(new[] { row })));
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

        throw new InvalidOperationException($"Expected {typeof(T).Name} when CSV output exceeds the supported UTF-8 budget.");
    }
}

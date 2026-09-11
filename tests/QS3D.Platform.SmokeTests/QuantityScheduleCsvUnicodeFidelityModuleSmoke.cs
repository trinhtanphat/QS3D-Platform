using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleCsvUnicodeFidelityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        RejectsMalformedHighSurrogate();
        RejectsMalformedLowSurrogate();
        PreservesSupplementaryUnicode();
        Console.WriteLine("PASS quantity schedule CSV Unicode fidelity");
    }

    private static void RejectsMalformedHighSurrogate()
    {
        Throws<InvalidOperationException>(() => WriteSingle("Wall\uD800", "WALL.AREA", "Wall Family"));
    }

    private static void RejectsMalformedLowSurrogate()
    {
        Throws<InvalidOperationException>(() => WriteSingle("Wall", "WALL.AREA\uDC00", "Wall Family"));
    }

    private static void PreservesSupplementaryUnicode()
    {
        var elementName = "Wall " + char.ConvertFromUtf32(0x1F30A);
        var familyName = "Family " + char.ConvertFromUtf32(0x1F3D7);
        var csv = WriteSingle(elementName, "WALL.AREA", familyName);
        if (!csv.Contains(elementName, StringComparison.Ordinal))
            throw new InvalidOperationException("Quantity CSV changed valid supplementary Unicode in element identity text.");
        if (!csv.Contains(familyName, StringComparison.Ordinal))
            throw new InvalidOperationException("Quantity CSV changed valid supplementary Unicode in family provenance text.");
    }

    private static string WriteSingle(string elementName, string code, string familyName)
    {
        var row = new QuantityScheduleRow(
            new ElementId(Guid.Parse("00000000-0000-0000-0000-00000000c201")),
            elementName,
            SemanticElementKind.Wall,
            new FamilyId(Guid.Parse("11111111-1111-1111-1111-11111111c201")),
            familyName,
            null,
            null,
            new[] { new QuantitySummary(code, QuantityDimension.Area, 12.5d, 1, 1) });
        return QuantityScheduleCsv.Write(new QuantitySchedule(new[] { row }));
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

        throw new InvalidOperationException($"Expected {typeof(T).Name} for malformed Quantity CSV UTF-16 text.");
    }
}

using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleCsvSecurityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        AssertNeutralized("=1+1", "COUNT");
        AssertNeutralized("+SUM(A1:A2)", "COUNT");
        AssertNeutralized("-2+3", "COUNT");
        AssertNeutralized("@SUM(A1:A2)", "COUNT");
        AssertNeutralized("  =1+1", "COUNT");
        AssertNeutralized("\t=1+1", "COUNT");
        AssertNeutralized("Safe", "=HYPERLINK(\"https://example.invalid\")");
        AssertNeutralized("=cmd,\"quoted\"\nline", "COUNT");

        var benign = WriteSingle("Wall-01", "WALL.AREA");
        if (!benign.Contains(",Wall-01,WALL.AREA,Area,12.5,m2", StringComparison.Ordinal))
            throw new InvalidOperationException("Benign quantity CSV fields changed unexpectedly.");
        AssertCanonicalCrLf(benign);

        Console.WriteLine("PASS quantity schedule CSV spreadsheet safety");
    }

    private static void AssertNeutralized(string elementName, string code)
    {
        var csv = WriteSingle(elementName, code);
        if (!csv.Contains("'" + elementName.Replace("\"", "\"\""), StringComparison.Ordinal) &&
            !csv.Contains("'" + code.Replace("\"", "\"\""), StringComparison.Ordinal))
            throw new InvalidOperationException($"CSV did not neutralize spreadsheet-active text for element '{elementName}' and code '{code}'.");
        AssertCanonicalCrLf(csv);
    }

    private static void AssertCanonicalCrLf(string csv)
    {
        if (!csv.EndsWith("\r\n", StringComparison.Ordinal))
            throw new InvalidOperationException("Quantity CSV must end with canonical CRLF.");
        for (var index = 0; index < csv.Length; index++)
        {
            if (csv[index] == '\n' && (index == 0 || csv[index - 1] != '\r'))
                throw new InvalidOperationException("Quantity CSV contains a non-canonical LF line ending.");
            if (csv[index] == '\r' && (index + 1 >= csv.Length || csv[index + 1] != '\n'))
                throw new InvalidOperationException("Quantity CSV contains a non-canonical CR line ending.");
        }
    }

    private static string WriteSingle(string elementName, string code)
    {
        var row = new QuantityScheduleRow(
            ElementId.New(),
            elementName,
            SemanticElementKind.Wall,
            FamilyId.New(),
            "Wall Family",
            null,
            null,
            new[] { new QuantitySummary(code, QuantityDimension.Area, 12.5d, 1, 1) });
        return QuantityScheduleCsv.Write(new QuantitySchedule(new[] { row }));
    }
}

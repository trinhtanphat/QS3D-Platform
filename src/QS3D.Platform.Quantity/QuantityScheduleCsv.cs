using System.Globalization;
using System.Text;

namespace QS3D.Platform.Quantity;

public static class QuantityScheduleCsv
{
    private const string CsvLineEnding = "\r\n";

    public static string Write(QuantitySchedule schedule)
    {
        if (schedule is null) throw new ArgumentNullException(nameof(schedule));
        EnsureOutputRecordCardinality(schedule);

        var output = new StringBuilder();
        output.Append("ElementId,ElementName,Code,Dimension,Value,CanonicalUnit,ElementKind,FamilyId,FamilyName,FloorId,ZoneId,FactCount,ElementCount,SourceDrawingId,SourceHandle");
        output.Append(CsvLineEnding);
        foreach (var row in schedule.Rows.OrderBy(static row => row.ElementId.Value))
        {
            if (row.Quantities.Count == 0)
            {
                Append(output, row.ElementId.Value.ToString("D", CultureInfo.InvariantCulture));
                Append(output, NeutralizeSpreadsheetActiveText(row.ElementName));
                Append(output, string.Empty);
                Append(output, string.Empty);
                Append(output, string.Empty);
                Append(output, string.Empty);
                AppendProvenance(output, row);
                Append(output, string.Empty);
                Append(output, string.Empty);
                AppendSourceProvenance(output, row);
                continue;
            }

            foreach (var summary in row.Quantities.OrderBy(static summary => summary.Code, StringComparer.Ordinal))
            {
                Append(output, row.ElementId.Value.ToString("D", CultureInfo.InvariantCulture));
                Append(output, NeutralizeSpreadsheetActiveText(row.ElementName));
                Append(output, NeutralizeSpreadsheetActiveText(summary.Code));
                Append(output, summary.Quantity.Dimension.ToString());
                Append(output, summary.Quantity.Value.ToString("R", CultureInfo.InvariantCulture));
                Append(output, CanonicalSymbol(summary.Quantity.Dimension));
                AppendProvenance(output, row);
                Append(output, summary.FactCount.ToString(CultureInfo.InvariantCulture));
                Append(output, summary.ElementCount.ToString(CultureInfo.InvariantCulture));
                AppendSourceProvenance(output, row);
            }
        }
        return output.ToString();
    }

    private static void AppendProvenance(StringBuilder output, QuantityScheduleRow row)
    {
        Append(output, row.ElementKind.ToString());
        Append(output, row.FamilyId.Value.ToString("D", CultureInfo.InvariantCulture));
        Append(output, NeutralizeSpreadsheetActiveText(row.FamilyName));
        Append(
            output,
            row.FloorId.HasValue
                ? row.FloorId.Value.Value.ToString("D", CultureInfo.InvariantCulture)
                : string.Empty);
        Append(
            output,
            row.ZoneId.HasValue
                ? row.ZoneId.Value.Value.ToString("D", CultureInfo.InvariantCulture)
                : string.Empty);
    }

    private static void AppendSourceProvenance(StringBuilder output, QuantityScheduleRow row)
    {
        if (row.SourceReference.HasValue)
        {
            var source = row.SourceReference.Value;
            Append(output, source.DrawingId.Value.ToString("D", CultureInfo.InvariantCulture));
            Append(output, source.Handle.Value, last: true);
            return;
        }

        Append(output, string.Empty);
        Append(output, string.Empty, last: true);
    }

    private static void EnsureOutputRecordCardinality(QuantitySchedule schedule)
    {
        var recordCount = 0;
        foreach (var row in schedule.Rows)
        {
            var rowRecordCount = row.Quantities.Count == 0 ? 1 : row.Quantities.Count;
            if (recordCount > QuantityScheduleMaterializer.MaximumEntries - rowRecordCount)
                throw new InvalidOperationException($"CSV data records exceed the supported maximum of {QuantityScheduleMaterializer.MaximumEntries} entries.");
            recordCount += rowRecordCount;
        }
    }

    private static string NeutralizeSpreadsheetActiveText(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));

        var firstNonWhitespace = 0;
        while (firstNonWhitespace < value.Length && char.IsWhiteSpace(value[firstNonWhitespace]))
            firstNonWhitespace++;

        if (firstNonWhitespace == value.Length) return value;

        switch (value[firstNonWhitespace])
        {
            case '=':
            case '+':
            case '-':
            case '@':
                return "'" + value;
            default:
                return value;
        }
    }

    private static void Append(StringBuilder output, string value, bool last = false)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        value = NormalizeLineEndings(value);
        var mustQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
        if (mustQuote)
        {
            output.Append('"');
            output.Append(value.Replace("\"", "\"\""));
            output.Append('"');
        }
        else
        {
            output.Append(value);
        }
        if (last) output.Append(CsvLineEnding);
        else output.Append(',');
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", CsvLineEnding);

    private static string CanonicalSymbol(QuantityDimension dimension)
    {
        switch (dimension)
        {
            case QuantityDimension.Count: return "ea";
            case QuantityDimension.Length: return "m";
            case QuantityDimension.Area: return "m2";
            case QuantityDimension.Volume: return "m3";
            case QuantityDimension.Mass: return "kg";
            default: throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unsupported quantity dimension.");
        }
    }
}

using System.Globalization;
using System.Text;

namespace QS3D.Platform.Quantity;

public static class QuantityScheduleCsv
{
    private const string CsvLineEnding = "\r\n";

    public static string Write(QuantitySchedule schedule)
    {
        if (schedule is null) throw new ArgumentNullException(nameof(schedule));
        var output = new StringBuilder();
        output.Append("ElementId,ElementName,Code,Dimension,Value,CanonicalUnit");
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
                Append(output, string.Empty, last: true);
                continue;
            }

            foreach (var summary in row.Quantities.OrderBy(static summary => summary.Code, StringComparer.Ordinal))
            {
                Append(output, row.ElementId.Value.ToString("D", CultureInfo.InvariantCulture));
                Append(output, NeutralizeSpreadsheetActiveText(row.ElementName));
                Append(output, NeutralizeSpreadsheetActiveText(summary.Code));
                Append(output, summary.Quantity.Dimension.ToString());
                Append(output, summary.Quantity.Value.ToString("R", CultureInfo.InvariantCulture));
                Append(output, CanonicalSymbol(summary.Quantity.Dimension), last: true);
            }
        }
        return output.ToString();
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

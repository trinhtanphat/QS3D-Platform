using System.Globalization;
using System.Text;

namespace QS3D.Platform.Quantity;

public static class QuantityScheduleCsv
{
    private const string CsvLineEnding = "\r\n";
    private const long MaxOutputUtf8Bytes = 16L * 1024L * 1024L;
    private const string Header = "ElementId,ElementName,Code,Dimension,Value,CanonicalUnit,ElementKind,FamilyId,FamilyName,FloorId,ZoneId,FactCount,ElementCount,SourceDrawingId,SourceHandle";

    public static string Write(QuantitySchedule schedule)
    {
        if (schedule is null) throw new ArgumentNullException(nameof(schedule));
        EnsureOutputRecordCardinality(schedule);

        var output = new CsvOutputBuilder(MaxOutputUtf8Bytes);
        output.AppendLiteral(Header);
        output.AppendLiteral(CsvLineEnding);
        foreach (var row in schedule.Rows.OrderBy(static row => row.ElementId.Value))
        {
            if (row.Quantities.Count == 0)
            {
                Append(output, row.ElementId.Value.ToString("D", CultureInfo.InvariantCulture));
                Append(output, row.ElementName, neutralizeSpreadsheetActiveText: true);
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
                Append(output, row.ElementName, neutralizeSpreadsheetActiveText: true);
                Append(output, summary.Code, neutralizeSpreadsheetActiveText: true);
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

    private static void AppendProvenance(CsvOutputBuilder output, QuantityScheduleRow row)
    {
        Append(output, row.ElementKind.ToString());
        Append(output, row.FamilyId.Value.ToString("D", CultureInfo.InvariantCulture));
        Append(output, row.FamilyName, neutralizeSpreadsheetActiveText: true);
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

    private static void AppendSourceProvenance(CsvOutputBuilder output, QuantityScheduleRow row)
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

    private static void Append(
        CsvOutputBuilder output,
        string value,
        bool last = false,
        bool neutralizeSpreadsheetActiveText = false)
    {
        if (output is null) throw new ArgumentNullException(nameof(output));
        if (value is null) throw new ArgumentNullException(nameof(value));
        output.AppendField(value, neutralizeSpreadsheetActiveText, last);
    }

    private static bool RequiresSpreadsheetNeutralization(string value)
    {
        var firstNonWhitespace = 0;
        while (firstNonWhitespace < value.Length && char.IsWhiteSpace(value[firstNonWhitespace]))
            firstNonWhitespace++;

        if (firstNonWhitespace == value.Length) return false;
        switch (value[firstNonWhitespace])
        {
            case '=':
            case '+':
            case '-':
            case '@':
                return true;
            default:
                return false;
        }
    }

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

    private sealed class CsvOutputBuilder
    {
        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly long _maxUtf8Bytes;
        private long _utf8Bytes;

        internal CsvOutputBuilder(long maxUtf8Bytes)
        {
            if (maxUtf8Bytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxUtf8Bytes));
            _maxUtf8Bytes = maxUtf8Bytes;
        }

        internal void AppendLiteral(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            Reserve(CountUtf8Bytes(value, 0, value.Length));
            _buffer.Append(value);
        }

        internal void AppendField(string value, bool neutralizeSpreadsheetActiveText, bool last)
        {
            var prependApostrophe = neutralizeSpreadsheetActiveText && RequiresSpreadsheetNeutralization(value);
            var mustQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            var emittedBytes = CountFieldUtf8Bytes(value, prependApostrophe, mustQuote, last);
            Reserve(emittedBytes);

            if (mustQuote) _buffer.Append('"');
            if (prependApostrophe) _buffer.Append('\'');
            AppendNormalizedEscapedValue(value);
            if (mustQuote) _buffer.Append('"');
            if (last) _buffer.Append(CsvLineEnding);
            else _buffer.Append(',');
        }

        public override string ToString() => _buffer.ToString();

        private void Reserve(long byteCount)
        {
            if (byteCount < 0) throw new InvalidOperationException("CSV output byte accounting overflowed.");
            if (byteCount > _maxUtf8Bytes - _utf8Bytes)
                throw new InvalidOperationException($"CSV output exceeds the supported UTF-8 maximum of {_maxUtf8Bytes} bytes.");
            _utf8Bytes += byteCount;
        }

        private static long CountFieldUtf8Bytes(string value, bool prependApostrophe, bool mustQuote, bool last)
        {
            var bytes = 0L;
            if (mustQuote) bytes = checked(bytes + 2L);
            if (prependApostrophe) bytes = checked(bytes + 1L);

            var segmentStart = 0;
            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];
                if (current != '"' && current != '\r' && current != '\n') continue;

                if (index > segmentStart)
                    bytes = checked(bytes + CountUtf8Bytes(value, segmentStart, index - segmentStart));

                if (current == '"')
                {
                    bytes = checked(bytes + 2L);
                    segmentStart = index + 1;
                    continue;
                }

                bytes = checked(bytes + 2L); // normalized CRLF
                if (current == '\r' && index + 1 < value.Length && value[index + 1] == '\n')
                    index++;
                segmentStart = index + 1;
            }

            if (segmentStart < value.Length)
                bytes = checked(bytes + CountUtf8Bytes(value, segmentStart, value.Length - segmentStart));

            return checked(bytes + (last ? 2L : 1L));
        }

        private static long CountUtf8Bytes(string value, int start, int count)
        {
            var end = checked(start + count);
            var bytes = 0L;
            for (var index = start; index < end; index++)
            {
                var current = value[index];
                if (current <= 0x7F)
                {
                    bytes = checked(bytes + 1L);
                    continue;
                }
                if (current <= 0x7FF)
                {
                    bytes = checked(bytes + 2L);
                    continue;
                }
                if (char.IsHighSurrogate(current))
                {
                    if (index + 1 < end && char.IsLowSurrogate(value[index + 1]))
                    {
                        bytes = checked(bytes + 4L);
                        index++;
                    }
                    else
                    {
                        bytes = checked(bytes + 3L); // UTF-8 replacement character
                    }
                    continue;
                }
                if (char.IsLowSurrogate(current))
                {
                    bytes = checked(bytes + 3L); // UTF-8 replacement character
                    continue;
                }
                bytes = checked(bytes + 3L);
            }
            return bytes;
        }

        private void AppendNormalizedEscapedValue(string value)
        {
            var segmentStart = 0;
            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];
                if (current != '"' && current != '\r' && current != '\n') continue;

                if (index > segmentStart)
                    _buffer.Append(value, segmentStart, index - segmentStart);

                if (current == '"')
                {
                    _buffer.Append("\"\"");
                    segmentStart = index + 1;
                    continue;
                }

                _buffer.Append(CsvLineEnding);
                if (current == '\r' && index + 1 < value.Length && value[index + 1] == '\n')
                    index++;
                segmentStart = index + 1;
            }

            if (segmentStart < value.Length)
                _buffer.Append(value, segmentStart, value.Length - segmentStart);
        }
    }
}

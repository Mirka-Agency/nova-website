using System.Globalization;
using System.Text;

namespace CMS.Modules.Shop.Application.Common;

public static class CsvHelper
{
    public sealed record CsvTable(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows);

    public static CsvTable Parse(Stream stream, int maxRows = 5000)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var records = ParseRecords(reader).ToList();
        if (records.Count == 0)
            return new CsvTable([], []);

        var headers = records[0]
            .Select(h => h.Trim())
            .ToList();

        var rows = new List<IReadOnlyList<string>>();
        foreach (var record in records.Skip(1))
        {
            if (IsEmptyRow(record))
                continue;

            if (rows.Count >= maxRows)
                throw new InvalidOperationException($"حداکثر {maxRows:N0} ردیف در هر فایل مجاز است.");

            rows.Add(NormalizeRow(record, headers.Count));
        }

        return new CsvTable(headers, rows);
    }

    public static byte[] Write(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
        {
            writer.WriteLine(string.Join(",", headers.Select(Escape)));
            foreach (var row in rows)
                writer.WriteLine(string.Join(",", row.Select(Escape)));
        }

        return stream.ToArray();
    }

    public static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
            return $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        return text;
    }

    public static bool TryGet(
        IReadOnlyDictionary<string, string> row,
        string column,
        out string? value)
    {
        if (row.TryGetValue(column, out var raw))
        {
            value = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
            return true;
        }

        value = null;
        return false;
    }

    public static bool TryParseBool(string? value, bool defaultValue, out bool result, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = defaultValue;
            error = null;
            return true;
        }

        var normalized = value.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "1":
            case "true":
            case "yes":
            case "y":
                result = true;
                error = null;
                return true;
            case "0":
            case "false":
            case "no":
            case "n":
                result = false;
                error = null;
                return true;
            default:
                result = defaultValue;
                error = $"مقدار بولی نامعتبر: {value}";
                return false;
        }
    }

    public static bool TryParseDecimal(string? value, out decimal result, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0;
            error = null;
            return true;
        }

        var normalized = value.Trim().Replace(" ", string.Empty);
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result)
            || decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.GetCultureInfo("fa-IR"), out result))
        {
            error = null;
            return true;
        }

        result = 0;
        error = $"عدد نامعتبر: {value}";
        return false;
    }

    public static bool TryParseInt(string? value, out int? result, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            error = null;
            return true;
        }

        var normalized = value.Trim().Replace(" ", string.Empty);
        if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            || int.TryParse(normalized, NumberStyles.Integer, CultureInfo.GetCultureInfo("fa-IR"), out parsed))
        {
            result = parsed;
            error = null;
            return true;
        }

        result = null;
        error = $"عدد صحیح نامعتبر: {value}";
        return false;
    }

    private static IEnumerable<List<string>> ParseRecords(TextReader reader)
    {
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        while (true)
        {
            var read = reader.Read();
            if (read == -1)
            {
                if (field.Length > 0 || row.Count > 0)
                {
                    row.Add(field.ToString());
                    if (!IsEmptyRow(row))
                        yield return row;
                }

                yield break;
            }

            var c = (char)read;
            if (inQuotes)
            {
                if (c == '"')
                {
                    var peek = reader.Peek();
                    if (peek == '"')
                    {
                        reader.Read();
                        field.Append('"');
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (c is '\r' or '\n')
            {
                if (c == '\r' && reader.Peek() == '\n')
                    reader.Read();

                row.Add(field.ToString());
                field.Clear();

                if (!IsEmptyRow(row))
                    yield return row;

                row = [];
            }
            else
            {
                field.Append(c);
            }
        }
    }

    private static IReadOnlyList<string> NormalizeRow(IReadOnlyList<string> record, int headerCount)
    {
        if (record.Count == headerCount)
            return record;

        if (record.Count < headerCount)
            return [.. record, .. Enumerable.Repeat(string.Empty, headerCount - record.Count)];

        return record.Take(headerCount).ToList();
    }

    private static bool IsEmptyRow(IEnumerable<string> record) =>
        record.All(string.IsNullOrWhiteSpace);

    public static IReadOnlyDictionary<string, string> ToRowDictionary(
        IReadOnlyList<string> headers,
        IReadOnlyList<string> values)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Trim();
            if (string.IsNullOrWhiteSpace(header))
                continue;

            row[header] = i < values.Count ? values[i] : string.Empty;
        }

        return row;
    }
}

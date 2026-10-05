using System.Globalization;
using System.Text.RegularExpressions;
using CMS.Application.Common.Time;
using CMS.Modules.Forms.Application.Fields;

namespace CMS.Modules.Forms.Application.Common;

/// <summary>Jalali (Persian) calendar helpers for form date/datetime fields.</summary>
public static class JalaliDateHelper
{
    private static readonly Regex JalaliDateRegex = new(
        @"^(?<y>\d{4})/(?<m>\d{1,2})/(?<d>\d{1,2})(?:\s+(?<h>\d{1,2}):(?<min>\d{1,2})(?::(?<s>\d{1,2}))?)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] GregorianDateFormats =
    [
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "yyyy-MM-ddTHH:mm",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy/MM/dd HH:mm",
        "yyyy/MM/dd HH:mm:ss"
    ];

    public static string NormalizeDigits(string value)
    {
        var buffer = value.ToCharArray();
        for (var i = 0; i < buffer.Length; i++)
        {
            var c = buffer[i];
            if (c is >= '\u06F0' and <= '\u06F9')
                buffer[i] = (char)('0' + (c - '\u06F0'));
            else if (c is >= '\u0660' and <= '\u0669')
                buffer[i] = (char)('0' + (c - '\u0660'));
        }

        return new string(buffer);
    }

    public static string? NormalizeInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = NormalizeDigits(value.Trim())
            .Replace('-', '/')
            .Replace('T', ' ');
        while (normalized.Contains("  ", StringComparison.Ordinal))
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static bool TryParse(string? value, out DateTime local)
    {
        local = default;
        var normalized = NormalizeInput(value);
        if (normalized is null)
            return false;

        if (TryParseJalali(normalized, out local))
            return true;

        // Restore ISO-ish separators for Gregorian parse.
        var gregorianCandidate = value!.Trim();
        gregorianCandidate = NormalizeDigits(gregorianCandidate);
        if (DateTime.TryParseExact(
                gregorianCandidate,
                GregorianDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                out local))
            return true;

        return DateTime.TryParse(gregorianCandidate, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out local);
    }

    public static bool TryParseJalali(string? value, out DateTime local)
    {
        local = default;
        var normalized = NormalizeInput(value);
        if (normalized is null)
            return false;

        var match = JalaliDateRegex.Match(normalized);
        if (!match.Success)
            return false;

        var year = int.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups["d"].Value, CultureInfo.InvariantCulture);
        var hour = match.Groups["h"].Success
            ? int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture)
            : 0;
        var minute = match.Groups["min"].Success
            ? int.Parse(match.Groups["min"].Value, CultureInfo.InvariantCulture)
            : 0;
        var second = match.Groups["s"].Success
            ? int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture)
            : 0;

        if (year is < 1200 or > 1600)
            return false;

        try
        {
            local = new PersianCalendar().ToDateTime(year, month, day, hour, minute, second, 0);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public static string Format(DateTime value, bool includeTime)
    {
        var persian = new PersianCalendar();
        if (includeTime)
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"{persian.GetYear(value):0000}/{persian.GetMonth(value):00}/{persian.GetDayOfMonth(value):00} {value.Hour:00}:{value.Minute:00}");
        }

        return string.Create(CultureInfo.InvariantCulture,
            $"{persian.GetYear(value):0000}/{persian.GetMonth(value):00}/{persian.GetDayOfMonth(value):00}");
    }

    public static string FormatFromUtc(DateTime utc, bool includeTime = true)
        => Format(IranTime.FromUtc(utc), includeTime);

    /// <summary>
    /// Formats a stored field value for admin/public display.
    /// Jalali strings are shown as-is; Gregorian ISO values are converted.
    /// </summary>
    public static string? FormatStoredValue(string? value, string? fieldTypeId)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var type = (fieldTypeId ?? string.Empty).Trim().ToLowerInvariant();
        var includeTime = type is FormFieldTypeIds.DateTime;
        var isDateLike = type is FormFieldTypeIds.Date or FormFieldTypeIds.DateTime;
        if (!isDateLike)
            return value;

        if (TryParseJalali(value, out _))
            return NormalizeInput(value);

        if (TryParse(value, out var parsed))
            return Format(parsed, includeTime);

        return value;
    }

    /// <summary>Value shown in public inputs (prefer Jalali).</summary>
    public static string? ToInputValue(string? value, bool includeTime)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (TryParseJalali(value, out _))
            return NormalizeInput(value);

        if (TryParse(value, out var parsed))
            return Format(parsed, includeTime);

        return value.Trim();
    }
}

using System.Globalization;

namespace CMS.Application.Formatting;

/// <summary>Persian relative time labels (e.g. «۱ هفته قبل», «۲ ماه قبل»).</summary>
public static class PersianRelativeDate
{
    public static string Format(DateTime value, DateTime? now = null)
    {
        var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        var nowLocal = now ?? DateTime.Now;
        var span = nowLocal - local;
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;

        if (span.TotalMinutes < 1)
            return "همین الان";

        if (span.TotalHours < 1)
            return FormatUnit(Math.Max(1, (int)span.TotalMinutes), "دقیقه");

        if (span.TotalHours < 24)
            return FormatUnit(Math.Max(1, (int)span.TotalHours), "ساعت");

        if (span.TotalDays < 7)
            return FormatUnit(Math.Max(1, (int)span.TotalDays), "روز");

        if (span.TotalDays < 30)
            return FormatUnit(Math.Max(1, (int)(span.TotalDays / 7)), "هفته");

        if (span.TotalDays < 365)
            return FormatUnit(Math.Max(1, (int)(span.TotalDays / 30)), "ماه");

        return FormatUnit(Math.Max(1, (int)(span.TotalDays / 365)), "سال");
    }

    private static string FormatUnit(int count, string unit) =>
        $"{ToPersianDigits(count)} {unit} قبل";

    private static string ToPersianDigits(int value) =>
        value.ToString(CultureInfo.InvariantCulture)
            .Replace('0', '۰')
            .Replace('1', '۱')
            .Replace('2', '۲')
            .Replace('3', '۳')
            .Replace('4', '۴')
            .Replace('5', '۵')
            .Replace('6', '۶')
            .Replace('7', '۷')
            .Replace('8', '۸')
            .Replace('9', '۹');
}

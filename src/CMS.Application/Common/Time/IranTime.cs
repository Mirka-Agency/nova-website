using System.Globalization;

namespace CMS.Application.Common.Time;

/// <summary>
/// Iran wall-clock helpers. Persist UTC in the database; convert here for UI / email / logs.
/// </summary>
public static class IranTime
{
    public static TimeZoneInfo TimeZone { get; } = Resolve();

    public static DateTime FromUtc(DateTime utc)
    {
        var value = utc.Kind switch
        {
            DateTimeKind.Utc => utc,
            DateTimeKind.Local => utc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utc, DateTimeKind.Utc)
        };
        return TimeZoneInfo.ConvertTimeFromUtc(value, TimeZone);
    }

    public static string Format(DateTime utc, string format = "yyyy/MM/dd HH:mm")
        => FromUtc(utc).ToString(format, CultureInfo.InvariantCulture);

    public static string FormatDate(DateTime utc)
        => Format(utc, "yyyy/MM/dd");

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "Asia/Tehran", "Iran Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Asia/Tehran",
            TimeSpan.FromHours(3.5),
            "Iran Standard Time",
            "Iran Standard Time");
    }
}

using System.Globalization;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Shop.Infrastructure.Services;

internal static class OrderNumberGenerator
{
    private const int MaxDailySequence = 999_999;
    private static readonly TimeZoneInfo IranTimeZone = ResolveIranTimeZone();

    /// <summary>
    /// Short daily order number, e.g. 250828-00123 (yyMMdd + 5-digit sequence, up to 999999/day).
    /// </summary>
    public static async Task<string> GenerateAsync(ShopDbContext db, CancellationToken cancellationToken)
    {
        var prefix = GetDailyPrefix();
        var prefixWithDash = prefix + "-";

        var lastNumber = await db.Orders.AsNoTracking()
            .Where(o => o.OrderNumber.StartsWith(prefixWithDash))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (lastNumber is not null && TryParseSequence(prefix, lastNumber, out var lastSequence))
            next = lastSequence + 1;

        if (next > MaxDailySequence)
            throw new DomainException("ظرفیت شماره‌گذاری سفارش برای امروز تکمیل شده است.");

        return $"{prefix}-{next:D5}";
    }

    private static string GetDailyPrefix()
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IranTimeZone);
        return local.ToString("yyMMdd", CultureInfo.InvariantCulture);
    }

    private static bool TryParseSequence(string prefix, string orderNumber, out int sequence)
    {
        sequence = 0;
        if (!orderNumber.StartsWith(prefix + "-", StringComparison.Ordinal))
            return false;

        var suffix = orderNumber[(prefix.Length + 1)..];
        return int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out sequence);
    }

    private static TimeZoneInfo ResolveIranTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "Iran Standard Time" : "Asia/Tehran");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                "Asia/Tehran",
                TimeSpan.FromHours(3.5),
                "Iran Standard Time",
                "Iran Standard Time");
        }
    }
}

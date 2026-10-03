using CMS.Application.Messaging;
using CMS.Infrastructure.Auth;
using CMS.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace CMS.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public class MessageLogsController : Controller
{
    private static readonly TimeZoneInfo IranTz = ResolveIranTimeZone();

    private readonly IMessageLogQueryService _messageLogs;
    private readonly IStringLocalizer<AdminShared> _localizer;

    public MessageLogsController(
        IMessageLogQueryService messageLogs,
        IStringLocalizer<AdminShared> localizer)
    {
        _messageLogs = messageLogs;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        var selectedPeriod = NormalizePeriod(period, from, to);
        var (fromUtc, toUtc, fromLocal, toLocal) = ResolveRange(selectedPeriod, from, to);

        ViewData["Title"] = _localizer["MessageLogs"].Value;
        var stats = await _messageLogs.GetStatsAsync(fromUtc, toUtc, cancellationToken);
        var recent = await _messageLogs.ListRecentAsync(50, cancellationToken);

        return View(new MessageLogsDashboardViewModel
        {
            Period = selectedPeriod,
            FromDate = fromLocal,
            ToDate = toLocal,
            Stats = stats,
            Recent = recent
        });
    }

    private static string NormalizePeriod(string? period, DateTime? from, DateTime? to)
    {
        if (string.Equals(period, "custom", StringComparison.OrdinalIgnoreCase) || (from.HasValue && to.HasValue))
            return "custom";

        return period?.Trim().ToLowerInvariant() switch
        {
            "today" => "today",
            "30d" => "30d",
            "90d" => "90d",
            _ => "7d"
        };
    }

    private static (DateTime FromUtc, DateTime ToUtc, DateTime FromLocal, DateTime ToLocal) ResolveRange(
        string period,
        DateTime? from,
        DateTime? to)
    {
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IranTz);
        var today = DateOnly.FromDateTime(nowLocal);

        DateOnly fromDate;
        DateOnly toDateInclusive;

        switch (period)
        {
            case "today":
                fromDate = today;
                toDateInclusive = today;
                break;
            case "30d":
                fromDate = today.AddDays(-29);
                toDateInclusive = today;
                break;
            case "90d":
                fromDate = today.AddDays(-89);
                toDateInclusive = today;
                break;
            case "custom":
                fromDate = DateOnly.FromDateTime((from ?? nowLocal.AddDays(-6)).Date);
                toDateInclusive = DateOnly.FromDateTime((to ?? nowLocal).Date);
                if (toDateInclusive < fromDate)
                    (fromDate, toDateInclusive) = (toDateInclusive, fromDate);
                break;
            default:
                fromDate = today.AddDays(-6);
                toDateInclusive = today;
                break;
        }

        var fromLocal = fromDate.ToDateTime(TimeOnly.MinValue);
        var toLocalExclusive = toDateInclusive.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(fromLocal, DateTimeKind.Unspecified), IranTz);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(toLocalExclusive, DateTimeKind.Unspecified), IranTz);

        return (fromUtc, toUtc, fromLocal, toDateInclusive.ToDateTime(TimeOnly.MinValue));
    }

    private static TimeZoneInfo ResolveIranTimeZone()
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
            "Iran Standard Time",
            TimeSpan.FromHours(3.5),
            "Iran Standard Time",
            "Iran Standard Time");
    }
}

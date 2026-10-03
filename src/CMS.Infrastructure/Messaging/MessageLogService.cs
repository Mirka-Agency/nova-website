using CMS.Application.Messaging;
using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Infrastructure.Messaging;

public sealed class MessageLogger : IMessageLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessageLogger> _logger;

    public MessageLogger(IServiceScopeFactory scopeFactory, ILogger<MessageLogger> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task LogSmsAsync(
        string recipient,
        string text,
        MessageSendStatus status,
        string? provider = null,
        string? providerMessageId = null,
        string? errorMessage = null,
        int recipientCount = 1,
        CancellationToken cancellationToken = default)
    {
        var entry = MessageLog.CreateSms(
            recipient,
            text,
            status,
            provider,
            providerMessageId,
            errorMessage,
            recipientCount);
        return PersistAsync(entry, cancellationToken);
    }

    public Task LogEmailAsync(
        string recipient,
        string subject,
        string body,
        MessageSendStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var entry = MessageLog.CreateEmail(recipient, subject, body, status, errorMessage);
        return PersistAsync(entry, cancellationToken);
    }

    private async Task PersistAsync(MessageLog entry, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.MessageLogs.Add(entry);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist message log. Channel={Channel} Recipient={Recipient} Status={Status}",
                entry.Channel,
                entry.Recipient,
                entry.Status);
        }
    }
}

public sealed class MessageLogQueryService : IMessageLogQueryService
{
    private readonly ApplicationDbContext _db;

    public MessageLogQueryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<MessageLogPeriodStatsDto> GetStatsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        if (toUtc < fromUtc)
            (fromUtc, toUtc) = (toUtc, fromUtc);

        var rows = await _db.MessageLogs
            .AsNoTracking()
            .Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc < toUtc)
            .Select(x => new { x.Channel, x.Status, x.RecipientCount, x.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        var smsSucceeded = rows.Where(x => x.Channel == MessageChannel.Sms && x.Status == MessageSendStatus.Succeeded)
            .Sum(x => x.RecipientCount);
        var smsFailed = rows.Count(x => x.Channel == MessageChannel.Sms && x.Status == MessageSendStatus.Failed);
        var smsSkipped = rows.Count(x => x.Channel == MessageChannel.Sms && x.Status == MessageSendStatus.Skipped);

        var emailSucceeded = rows.Where(x => x.Channel == MessageChannel.Email && x.Status == MessageSendStatus.Succeeded)
            .Sum(x => x.RecipientCount);
        var emailFailed = rows.Count(x => x.Channel == MessageChannel.Email && x.Status == MessageSendStatus.Failed);
        var emailSkipped = rows.Count(x => x.Channel == MessageChannel.Email && x.Status == MessageSendStatus.Skipped);

        var tz = MessageLogTimeZones.Iran;
        var dailyMap = rows
            .Where(x => x.Status == MessageSendStatus.Succeeded)
            .GroupBy(x => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(x.CreatedAtUtc, tz)))
            .ToDictionary(
                g => g.Key,
                g => (
                    Sms: g.Where(x => x.Channel == MessageChannel.Sms).Sum(x => x.RecipientCount),
                    Email: g.Where(x => x.Channel == MessageChannel.Email).Sum(x => x.RecipientCount)));

        var fromLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(fromUtc, tz));
        var toLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(toUtc.AddTicks(-1), tz));
        if (toLocal < fromLocal)
            toLocal = fromLocal;

        var daily = new List<MessageLogDailyStatDto>();
        for (var day = fromLocal; day <= toLocal; day = day.AddDays(1))
        {
            dailyMap.TryGetValue(day, out var counts);
            daily.Add(new MessageLogDailyStatDto(day, counts.Sms, counts.Email));
        }

        return new MessageLogPeriodStatsDto(
            fromUtc,
            toUtc,
            smsSucceeded,
            smsFailed,
            smsSkipped,
            emailSucceeded,
            emailFailed,
            emailSkipped,
            daily);
    }

    public async Task<IReadOnlyList<MessageLogDto>> ListRecentAsync(
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(take, 1, 500);
        return await _db.MessageLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .Select(x => new MessageLogDto(
                x.Id,
                x.Channel,
                x.Status,
                x.Recipient,
                x.RecipientCount,
                x.Subject,
                x.BodyPreview,
                x.Provider,
                x.ProviderMessageId,
                x.ErrorMessage,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}

internal static class MessageLogTimeZones
{
    public static TimeZoneInfo Iran { get; } = ResolveIran();

    private static TimeZoneInfo ResolveIran()
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

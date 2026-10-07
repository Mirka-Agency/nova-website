namespace CMS.Application.Messaging;

public enum MessageChannel : byte
{
    Sms = 1,
    Email = 2,
    WhatsApp = 3
}

public enum MessageSendStatus : byte
{
    Succeeded = 1,
    Failed = 2,
    Skipped = 3
}

public sealed record MessageLogDto(
    Guid Id,
    MessageChannel Channel,
    MessageSendStatus Status,
    string Recipient,
    int RecipientCount,
    string? Subject,
    string? BodyPreview,
    string? Provider,
    string? ProviderMessageId,
    string? ErrorMessage,
    DateTime CreatedAtUtc);

public sealed record MessageLogDailyStatDto(
    DateOnly Date,
    int SmsSucceeded,
    int EmailSucceeded,
    int WhatsAppSucceeded = 0);

public sealed record MessageLogPeriodStatsDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int SmsSucceeded,
    int SmsFailed,
    int SmsSkipped,
    int EmailSucceeded,
    int EmailFailed,
    int EmailSkipped,
    IReadOnlyList<MessageLogDailyStatDto> Daily,
    int WhatsAppSucceeded = 0,
    int WhatsAppFailed = 0,
    int WhatsAppSkipped = 0);

public interface IMessageLogger
{
    Task LogSmsAsync(
        string recipient,
        string text,
        MessageSendStatus status,
        string? provider = null,
        string? providerMessageId = null,
        string? errorMessage = null,
        int recipientCount = 1,
        CancellationToken cancellationToken = default);

    Task LogEmailAsync(
        string recipient,
        string subject,
        string body,
        MessageSendStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    Task LogWhatsAppAsync(
        string recipient,
        string? formName,
        string body,
        MessageSendStatus status,
        string? providerMessageId = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}

public interface IMessageLogQueryService
{
    Task<MessageLogPeriodStatsDto> GetStatsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageLogDto>> ListRecentAsync(
        int take = 100,
        CancellationToken cancellationToken = default);
}

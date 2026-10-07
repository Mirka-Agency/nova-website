namespace CMS.Application.WhatsApp;

public static class WhatsAppConnectionStatuses
{
    public const string Connected = "Connected";
    public const string Disconnected = "Disconnected";
    public const string Connecting = "Connecting";
    public const string QrRequired = "QR Required";
    public const string AuthenticationFailed = "Authentication Failed";
}

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    /// <summary>Base URL of the internal WhatsApp microservice (e.g. http://whatsapp:3100).</summary>
    public string ServiceBaseUrl { get; set; } = "http://localhost:3100";

    /// <summary>Shared secret for X-Api-Key between CMS and WhatsApp service.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ServiceBaseUrl)
        && !string.IsNullOrWhiteSpace(ApiKey);
}

public sealed record WhatsAppStatusDto(
    string Status,
    string? PhoneNumber,
    string? PushName,
    DateTime? LastConnectedAtUtc,
    string? LastError,
    bool HasQr,
    bool Connected);

public sealed record WhatsAppQrDto(
    string Status,
    string? QrDataUrl);

public sealed record WhatsAppGroupDto(
    string Id,
    string Name,
    int? ParticipantsCount);

public sealed record WhatsAppSendResult(
    bool Succeeded,
    string? MessageId = null,
    string? ErrorMessage = null,
    bool Skipped = false)
{
    public static WhatsAppSendResult Skip(string reason) =>
        new(false, ErrorMessage: reason, Skipped: true);

    public static WhatsAppSendResult Ok(string? messageId) =>
        new(true, messageId);

    public static WhatsAppSendResult Fail(string reason) =>
        new(false, ErrorMessage: reason);
}

public sealed record WhatsAppSettingsDto(
    bool Enabled,
    string? DefaultGroupId,
    string? DefaultGroupName,
    string DefaultTemplate,
    DateTime? LastSuccessfulSendAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record UpdateWhatsAppSettingsCommand(
    bool Enabled,
    string? DefaultGroupId,
    string? DefaultGroupName,
    string? DefaultTemplate);

public interface IWhatsAppGateway
{
    Task<WhatsAppStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<WhatsAppQrDto> GetQrAsync(CancellationToken cancellationToken = default);
    Task<WhatsAppStatusDto> ReconnectAsync(CancellationToken cancellationToken = default);
    Task<WhatsAppStatusDto> LogoutAsync(CancellationToken cancellationToken = default);
    Task<WhatsAppStatusDto> ClearSessionAsync(CancellationToken cancellationToken = default);
    Task<WhatsAppStatusDto> RegenerateQrAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WhatsAppGroupDto>> ListGroupsAsync(CancellationToken cancellationToken = default);
    Task<WhatsAppSendResult> SendToGroupAsync(
        string groupId,
        string message,
        CancellationToken cancellationToken = default);
}

public interface IWhatsAppSettingsService
{
    Task<WhatsAppSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(UpdateWhatsAppSettingsCommand command, CancellationToken cancellationToken = default);
    Task MarkSuccessfulSendAsync(CancellationToken cancellationToken = default);
}

public interface IWhatsAppNotifier
{
    /// <summary>
    /// Sends a WhatsApp group message when global settings allow it.
    /// Never throws — failures are logged and returned.
    /// </summary>
    Task<WhatsAppSendResult> NotifyGroupAsync(
        string? groupId,
        string? groupName,
        string message,
        string? formName = null,
        CancellationToken cancellationToken = default);
}

public static class WhatsAppDefaultTemplate
{
    public const string Value =
        """
        📩 فرم جدید سایت

        فرم: {{form_name}}
        نام: {{name}}
        موبایل: {{phone}}
        تاریخ: {{date}}
        صفحه: {{page_url}}

        {{fields}}
        """;
}

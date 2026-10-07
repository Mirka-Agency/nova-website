using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CMS.Application.WhatsApp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.WhatsApp;

public sealed class WhatsAppGateway : IWhatsAppGateway
{
    public const string HttpClientName = "whatsapp-service";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppGateway> _logger;

    public WhatsAppGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppOptions> options,
        ILogger<WhatsAppGateway> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public Task<WhatsAppStatusDto> GetStatusAsync(CancellationToken cancellationToken = default) =>
        GetStatusFromAsync("/status", cancellationToken);

    public async Task<WhatsAppQrDto> GetQrAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var client = CreateClient();
        using var response = await client.GetAsync("/qr", cancellationToken);
        var payload = await ReadAsync<QrResponse>(response, cancellationToken);
        return new WhatsAppQrDto(
            payload?.Status ?? WhatsAppConnectionStatuses.Disconnected,
            payload?.QrDataUrl);
    }

    public Task<WhatsAppStatusDto> ReconnectAsync(CancellationToken cancellationToken = default) =>
        PostStatusAsync("/reconnect", cancellationToken);

    public Task<WhatsAppStatusDto> LogoutAsync(CancellationToken cancellationToken = default) =>
        PostStatusAsync("/logout", cancellationToken);

    public Task<WhatsAppStatusDto> ClearSessionAsync(CancellationToken cancellationToken = default) =>
        PostStatusAsync("/session/clear", cancellationToken);

    public Task<WhatsAppStatusDto> RegenerateQrAsync(CancellationToken cancellationToken = default) =>
        PostStatusAsync("/qr/regenerate", cancellationToken);

    public async Task<IReadOnlyList<WhatsAppGroupDto>> ListGroupsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var client = CreateClient();
        using var response = await client.GetAsync("/groups", cancellationToken);
        var payload = await ReadAsync<GroupsResponse>(response, cancellationToken);
        return (payload?.Groups ?? [])
            .Where(g => !string.IsNullOrWhiteSpace(g.Id))
            .Select(g => new WhatsAppGroupDto(
                g.Id!.Trim(),
                string.IsNullOrWhiteSpace(g.Name) ? g.Id.Trim() : g.Name.Trim(),
                g.ParticipantsCount))
            .ToList();
    }

    public async Task<WhatsAppSendResult> SendToGroupAsync(
        string groupId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            return WhatsAppSendResult.Fail("Group ID is required.");
        if (string.IsNullOrWhiteSpace(message))
            return WhatsAppSendResult.Fail("Message is required.");

        if (!_options.IsConfigured)
            return WhatsAppSendResult.Skip("WhatsApp service is not configured.");

        try
        {
            var client = CreateClient();
            using var response = await client.PostAsJsonAsync(
                "/send",
                new { groupId = groupId.Trim(), message = message.Trim() },
                JsonOptions,
                cancellationToken);

            var payload = await ReadAsync<SendResponse>(response, cancellationToken);
            if (!response.IsSuccessStatusCode || payload is null || !payload.Ok)
            {
                var error = payload?.Error
                            ?? $"WhatsApp send failed with status {(int)response.StatusCode}.";
                return WhatsAppSendResult.Fail(error);
            }

            return WhatsAppSendResult.Ok(payload.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp send request failed");
            return WhatsAppSendResult.Fail(ex.Message);
        }
    }

    private async Task<WhatsAppStatusDto> GetStatusFromAsync(string path, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var client = CreateClient();
        using var response = await client.GetAsync(path, cancellationToken);
        var payload = await ReadAsync<StatusResponse>(response, cancellationToken);
        return MapStatus(payload);
    }

    private async Task<WhatsAppStatusDto> PostStatusAsync(string path, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var client = CreateClient();
        using var response = await client.PostAsync(path, content: null, cancellationToken);
        var payload = await ReadAsync<StatusResponse>(response, cancellationToken);
        return MapStatus(payload);
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Remove("X-Api-Key");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", _options.ApiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("WhatsApp service is not configured (WhatsApp:ServiceBaseUrl / WhatsApp:ApiKey).");
    }

    private static WhatsAppStatusDto MapStatus(StatusResponse? payload) =>
        new(
            string.IsNullOrWhiteSpace(payload?.Status)
                ? WhatsAppConnectionStatuses.Disconnected
                : payload.Status.Trim(),
            NullIfWhiteSpace(payload?.PhoneNumber),
            NullIfWhiteSpace(payload?.PushName),
            ParseUtc(payload?.LastConnectedAt),
            NullIfWhiteSpace(payload?.LastError),
            payload?.HasQr == true,
            payload?.Connected == true
            || string.Equals(payload?.Status, WhatsAppConnectionStatuses.Connected, StringComparison.OrdinalIgnoreCase));

    private async Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to parse WhatsApp service response ({StatusCode})",
                (int)response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"WhatsApp service returned {(int)response.StatusCode}.");
            }

            return default;
        }
    }

    private static DateTime? ParseUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return DateTime.TryParse(
            value,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var dt)
            ? dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                : dt.ToUniversalTime()
            : null;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class StatusResponse
    {
        public string? Status { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PushName { get; set; }
        public string? LastConnectedAt { get; set; }
        public string? LastError { get; set; }
        public bool HasQr { get; set; }
        public bool Connected { get; set; }
        public string? Error { get; set; }
    }

    private sealed class QrResponse
    {
        public string? Status { get; set; }
        public string? QrDataUrl { get; set; }
        public string? Error { get; set; }
    }

    private sealed class GroupsResponse
    {
        public List<GroupItem>? Groups { get; set; }
        public string? Error { get; set; }
    }

    private sealed class GroupItem
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int? ParticipantsCount { get; set; }
    }

    private sealed class SendResponse
    {
        public bool Ok { get; set; }
        public string? MessageId { get; set; }
        public string? Error { get; set; }
    }
}

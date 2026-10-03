using CMS.Application.Sms;
using IPE.SmsIrClient;
using IPE.SmsIrClient.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Sms;

/// <summary>
/// sms.ir provider via the official <see cref="SmsIr"/> NuGet client (IPE.SmsIr).
/// All sends use <c>BulkSendAsync</c> (free-text line SMS).
/// </summary>
public sealed class SmsIrSmsProvider : ISmsProvider
{
    private readonly SmsIrOptions _options;
    private readonly ILogger<SmsIrSmsProvider> _logger;
    private readonly Lazy<SmsIr> _client;

    public SmsIrSmsProvider(
        IOptions<SmsIrOptions> options,
        ILogger<SmsIrSmsProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new Lazy<SmsIr>(() => new SmsIr(_options.ApiKey.Trim()));
    }

    public string ProviderId => SmsProviders.SmsIr;

    public bool IsConfigured => _options.IsConfigured;

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return SmsSendResult.Skip(ProviderId, "sms.ir is not configured (ApiKey / LineNumber).");

        ArgumentException.ThrowIfNullOrWhiteSpace(message.Mobile);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Text);

        var mobiles = BuildApiMobiles(message.Mobile, message.AdditionalMobiles);
        if (mobiles.Length == 0)
            return SmsSendResult.Fail(ProviderId, "No valid mobile numbers.");

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await _client.Value.BulkSendAsync(
                _options.LineNumber,
                message.Text,
                mobiles);

            var data = response.Data;
            var messageId = data?.PackId.ToString();
            if (data?.MessageIds is { Length: > 0 } ids)
            {
                var joined = string.Join(',', ids.Where(id => id.HasValue).Select(id => id!.Value));
                if (!string.IsNullOrWhiteSpace(joined))
                    messageId = joined;
            }

            _logger.LogInformation(
                "sms.ir BulkSend ok. Recipients={Count} PackId={PackId} Status={Status}",
                mobiles.Length,
                data?.PackId,
                response.Status);

            return SmsSendResult.Ok(ProviderId, messageId, response.Message);
        }
        catch (Exception ex) when (ex is SmsIrException or InvalidOperationException)
        {
            return FailFromException(ex, "BulkSend", string.Join(',', mobiles));
        }
    }

    private SmsSendResult FailFromException(Exception ex, string operation, string mobile)
    {
        var reason = DescribeException(ex);
        _logger.LogWarning(
            ex,
            "sms.ir {Operation} failed. Mobile={Mobile} Error={Error}",
            operation,
            mobile,
            reason);
        return SmsSendResult.Fail(ProviderId, reason);
    }

    private static string DescribeException(Exception ex) =>
        ex.GetType().Name switch
        {
            nameof(UnauthorizedException) or nameof(AccessDeniedException)
                => string.IsNullOrWhiteSpace(ex.Message)
                    ? "sms.ir authentication failed (invalid API key or access denied)."
                    : ex.Message,
            nameof(LogicalException)
                => string.IsNullOrWhiteSpace(ex.Message)
                    ? "sms.ir rejected the request (check LineNumber / mobiles / text)."
                    : ex.Message,
            nameof(TooManyRequestException)
                => string.IsNullOrWhiteSpace(ex.Message)
                    ? "sms.ir rate limit exceeded."
                    : ex.Message,
            _ => string.IsNullOrWhiteSpace(ex.Message)
                ? "sms.ir request failed."
                : ex.Message
        };

    private static string[] BuildApiMobiles(string primary, IReadOnlyList<string>? additional)
    {
        var list = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(string? raw)
        {
            var apiMobile = ToApiMobile(SmsPhoneNormalizer.Normalize(raw));
            if (apiMobile is null || !seen.Add(apiMobile))
                return;
            list.Add(apiMobile);
        }

        Add(primary);
        if (additional is not null)
        {
            foreach (var mobile in additional)
                Add(mobile);
        }

        return list.ToArray();
    }

    /// <summary>
    /// Official SDK samples use mobiles without a leading zero (e.g. 9120000000).
    /// </summary>
    private static string? ToApiMobile(string? normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        return normalized.StartsWith('0') && normalized.Length > 1
            ? normalized[1..]
            : normalized;
    }
}

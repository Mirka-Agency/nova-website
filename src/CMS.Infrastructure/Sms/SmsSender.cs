using CMS.Application.Messaging;
using CMS.Application.Sms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Sms;

/// <summary>Resolves the configured <see cref="ISmsProvider"/> and forwards send requests.</summary>
public sealed class SmsSender : ISmsSender
{
    private readonly IReadOnlyDictionary<string, ISmsProvider> _providers;
    private readonly SmsOptions _options;
    private readonly IMessageLogger _messageLogger;
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(
        IEnumerable<ISmsProvider> providers,
        IOptions<SmsOptions> options,
        IMessageLogger messageLogger,
        ILogger<SmsSender> logger)
    {
        _providers = providers.ToDictionary(p => p.ProviderId, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
        _messageLogger = messageLogger;
        _logger = logger;
    }

    private const string OptOutFooter = "لغو11";

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        var text = AppendOptOutFooter(message.Text);
        var withFooter = message with { Text = text };
        var result = await ExecuteAsync(p => p.SendAsync(withFooter, cancellationToken));
        await LogAsync(withFooter, result, cancellationToken);
        return result;
    }

    private static string AppendOptOutFooter(string text)
    {
        var trimmed = (text ?? string.Empty).TrimEnd();
        if (trimmed.EndsWith(OptOutFooter, StringComparison.Ordinal))
            return trimmed;

        return string.IsNullOrEmpty(trimmed)
            ? OptOutFooter
            : trimmed + "\n" + OptOutFooter;
    }

    private async Task<SmsSendResult> ExecuteAsync(Func<ISmsProvider, Task<SmsSendResult>> action)
    {
        var providerId = (_options.Provider ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(providerId))
        {
            _logger.LogWarning("SMS skipped (Sms:Provider is not set).");
            return SmsSendResult.Skip(SmsProviders.None, "SMS provider is not configured.");
        }

        if (!_providers.TryGetValue(providerId, out var provider))
        {
            _logger.LogError("Unknown SMS provider '{Provider}'. Registered: {Providers}",
                providerId,
                string.Join(", ", _providers.Keys));
            return SmsSendResult.Fail(providerId, $"Unknown SMS provider '{providerId}'.");
        }

        if (!provider.IsConfigured)
        {
            _logger.LogWarning("SMS skipped ({Provider} is not fully configured).", provider.ProviderId);
            return SmsSendResult.Skip(provider.ProviderId, $"{provider.ProviderId} is not configured.");
        }

        return await action(provider);
    }

    private Task LogAsync(SmsMessage message, SmsSendResult result, CancellationToken cancellationToken)
    {
        var recipients = BuildRecipientList(message);
        var status = result.Skipped
            ? MessageSendStatus.Skipped
            : result.Succeeded
                ? MessageSendStatus.Succeeded
                : MessageSendStatus.Failed;

        return _messageLogger.LogSmsAsync(
            recipients.Display,
            message.Text,
            status,
            result.Provider,
            result.MessageId,
            result.ProviderMessage,
            recipients.Count,
            cancellationToken);
    }

    private static (string Display, int Count) BuildRecipientList(SmsMessage message)
    {
        var list = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(string? raw)
        {
            var normalized = SmsPhoneNormalizer.Normalize(raw) ?? raw?.Trim();
            if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
                return;
            list.Add(normalized);
        }

        Add(message.Mobile);
        if (message.AdditionalMobiles is not null)
        {
            foreach (var mobile in message.AdditionalMobiles)
                Add(mobile);
        }

        return list.Count == 0
            ? (message.Mobile?.Trim() ?? string.Empty, 1)
            : (string.Join(", ", list), list.Count);
    }
}

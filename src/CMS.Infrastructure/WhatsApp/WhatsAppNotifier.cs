using CMS.Application.Messaging;
using CMS.Application.WhatsApp;
using Microsoft.Extensions.Logging;

namespace CMS.Infrastructure.WhatsApp;

public sealed class WhatsAppNotifier : IWhatsAppNotifier
{
    private readonly IWhatsAppGateway _gateway;
    private readonly IWhatsAppSettingsService _settings;
    private readonly IMessageLogger _messageLogger;
    private readonly ILogger<WhatsAppNotifier> _logger;

    public WhatsAppNotifier(
        IWhatsAppGateway gateway,
        IWhatsAppSettingsService settings,
        IMessageLogger messageLogger,
        ILogger<WhatsAppNotifier> logger)
    {
        _gateway = gateway;
        _settings = settings;
        _messageLogger = messageLogger;
        _logger = logger;
    }

    public async Task<WhatsAppSendResult> NotifyGroupAsync(
        string? groupId,
        string? groupName,
        string message,
        string? formName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settings.GetAsync(cancellationToken);
            if (!settings.Enabled)
            {
                var skipped = WhatsAppSendResult.Skip("WhatsApp notifications are disabled globally.");
                await LogAsync(groupId, groupName, message, formName, skipped, cancellationToken);
                return skipped;
            }

            var targetGroupId = string.IsNullOrWhiteSpace(groupId)
                ? settings.DefaultGroupId
                : groupId.Trim();
            var targetGroupName = string.IsNullOrWhiteSpace(groupName)
                ? settings.DefaultGroupName
                : groupName.Trim();

            if (string.IsNullOrWhiteSpace(targetGroupId))
            {
                var missing = WhatsAppSendResult.Fail("No WhatsApp group is selected.");
                await LogAsync(null, targetGroupName, message, formName, missing, cancellationToken);
                return missing;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                var empty = WhatsAppSendResult.Fail("Message is empty.");
                await LogAsync(targetGroupId, targetGroupName, message, formName, empty, cancellationToken);
                return empty;
            }

            var result = await _gateway.SendToGroupAsync(targetGroupId, message, cancellationToken);
            await LogAsync(targetGroupId, targetGroupName, message, formName, result, cancellationToken);

            if (result.Succeeded)
                await _settings.MarkSuccessfulSendAsync(cancellationToken);
            else if (!result.Skipped)
                _logger.LogWarning(
                    "WhatsApp notify failed for form {FormName}: {Error}",
                    formName,
                    result.ErrorMessage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp notify threw unexpectedly for form {FormName}", formName);
            var failed = WhatsAppSendResult.Fail(ex.Message);
            await LogAsync(groupId, groupName, message, formName, failed, cancellationToken);
            return failed;
        }
    }

    private Task LogAsync(
        string? groupId,
        string? groupName,
        string message,
        string? formName,
        WhatsAppSendResult result,
        CancellationToken cancellationToken)
    {
        var recipient = string.IsNullOrWhiteSpace(groupName)
            ? (groupId ?? "—")
            : $"{groupName} ({groupId})";

        var status = result.Skipped
            ? MessageSendStatus.Skipped
            : result.Succeeded
                ? MessageSendStatus.Succeeded
                : MessageSendStatus.Failed;

        return _messageLogger.LogWhatsAppAsync(
            recipient,
            formName,
            message,
            status,
            providerMessageId: result.MessageId,
            errorMessage: result.ErrorMessage,
            cancellationToken: cancellationToken);
    }
}

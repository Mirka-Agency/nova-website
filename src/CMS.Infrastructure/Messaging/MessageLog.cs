using CMS.Application.Messaging;
using CMS.Domain.Common;

namespace CMS.Infrastructure.Messaging;

public sealed class MessageLog : BaseEntity
{
    private MessageLog()
    {
    }

    public MessageChannel Channel { get; private set; }
    public MessageSendStatus Status { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public int RecipientCount { get; private set; } = 1;
    public string? Subject { get; private set; }
    public string? BodyPreview { get; private set; }
    public string? Provider { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static MessageLog CreateSms(
        string recipient,
        string? text,
        MessageSendStatus status,
        string? provider,
        string? providerMessageId,
        string? errorMessage,
        int recipientCount = 1)
    {
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient is required.", nameof(recipient));

        return new MessageLog
        {
            Channel = MessageChannel.Sms,
            Status = status,
            Recipient = NormalizeRequired(recipient, 500),
            RecipientCount = Math.Max(1, recipientCount),
            BodyPreview = Normalize(text, 500),
            Provider = Normalize(provider, 50),
            ProviderMessageId = Normalize(providerMessageId, 200),
            ErrorMessage = Normalize(errorMessage, 1000)
        };
    }

    public static MessageLog CreateEmail(
        string recipient,
        string subject,
        string? body,
        MessageSendStatus status,
        string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient is required.", nameof(recipient));

        return new MessageLog
        {
            Channel = MessageChannel.Email,
            Status = status,
            Recipient = NormalizeRequired(recipient, 500),
            RecipientCount = 1,
            Subject = Normalize(subject, 300) ?? string.Empty,
            BodyPreview = Normalize(body, 500),
            Provider = "Smtp",
            ErrorMessage = Normalize(errorMessage, 1000)
        };
    }

    public static MessageLog CreateWhatsApp(
        string recipient,
        string? formName,
        string? body,
        MessageSendStatus status,
        string? providerMessageId,
        string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient is required.", nameof(recipient));

        return new MessageLog
        {
            Channel = MessageChannel.WhatsApp,
            Status = status,
            Recipient = NormalizeRequired(recipient, 500),
            RecipientCount = 1,
            Subject = Normalize(formName, 300),
            BodyPreview = Normalize(body, 500),
            Provider = "WhatsAppWeb",
            ProviderMessageId = Normalize(providerMessageId, 200),
            ErrorMessage = Normalize(errorMessage, 1000)
        };
    }

    private static string NormalizeRequired(string value, int max)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string? Normalize(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}

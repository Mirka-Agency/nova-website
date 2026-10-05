using CMS.Application.Email;
using CMS.Application.Messaging;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CMS.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;
    private readonly IMessageLogger _messageLogger;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<SmtpEmailOptions> options,
        IMessageLogger messageLogger,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _messageLogger = messageLogger;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning(
                "Email skipped (SMTP not configured). To={To} Subject={Subject}",
                message.To,
                message.Subject);
            await _messageLogger.LogEmailAsync(
                message.To,
                message.Subject,
                message.Body,
                MessageSendStatus.Skipped,
                "SMTP is not configured.",
                cancellationToken);
            return;
        }

        try
        {
            var displayName = !string.IsNullOrWhiteSpace(message.FromDisplayName)
                ? message.FromDisplayName
                : (string.IsNullOrWhiteSpace(_options.FromDisplayName) ? null : _options.FromDisplayName);

            var mime = new MimeMessage();
            mime.From.Add(string.IsNullOrWhiteSpace(displayName)
                ? new MailboxAddress(string.Empty, _options.From)
                : new MailboxAddress(displayName, _options.From));
            mime.To.Add(MailboxAddress.Parse(message.To));
            mime.Subject = message.Subject;

            if (!string.IsNullOrWhiteSpace(message.ReplyTo))
                mime.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));

            mime.Body = message.IsHtml
                ? new TextPart("html") { Text = message.Body }
                : new TextPart("plain") { Text = message.Body };

            var socketOptions = ResolveSocketOptions(_options);

            using var client = new SmtpClient();
            // Many shared hosts (cPanel) present certs that fail strict chain validation in containers.
            client.ServerCertificateValidationCallback = static (_, _, _, _) => true;
            client.Timeout = 30_000;

            await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent to {To} subject {Subject}", message.To, message.Subject);
            await _messageLogger.LogEmailAsync(
                message.To,
                message.Subject,
                message.Body,
                MessageSendStatus.Succeeded,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email failed. To={To} Subject={Subject}", message.To, message.Subject);
            await _messageLogger.LogEmailAsync(
                message.To,
                message.Subject,
                message.Body,
                MessageSendStatus.Failed,
                ex.Message,
                cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Maps config to MailKit socket options.
    /// Prefer <see cref="SmtpEmailOptions.SecureSocketOptions"/>; otherwise derive from Port + EnableSsl.
    /// </summary>
    internal static SecureSocketOptions ResolveSocketOptions(SmtpEmailOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SecureSocketOptions)
            && Enum.TryParse<SecureSocketOptions>(options.SecureSocketOptions.Trim(), ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        if (!options.EnableSsl)
            return SecureSocketOptions.None;

        // Port 465 = implicit TLS; 587/25 = STARTTLS after greeting.
        return options.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }
}

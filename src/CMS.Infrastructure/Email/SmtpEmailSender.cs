using System.Net;
using System.Net.Mail;
using CMS.Application.Email;
using CMS.Application.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                client.Credentials = new NetworkCredential(_options.Username, _options.Password);
            }

            var displayName = !string.IsNullOrWhiteSpace(message.FromDisplayName)
                ? message.FromDisplayName
                : (string.IsNullOrWhiteSpace(_options.FromDisplayName) ? null : _options.FromDisplayName);

            using var mail = new MailMessage
            {
                From = new MailAddress(_options.From, displayName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsHtml
            };
            mail.To.Add(message.To);
            if (!string.IsNullOrWhiteSpace(message.ReplyTo))
                mail.ReplyToList.Add(message.ReplyTo);

            await client.SendMailAsync(mail, cancellationToken);
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
}

using CMS.Application.Email;
using CMS.Application.Sms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Notifications;

public sealed class AdminNotifier : IAdminNotifier, IAdminSmsNotifier
{
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;
    private readonly SmsOptions _smsOptions;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<AdminNotifier> _logger;

    public AdminNotifier(
        ISmsSender sms,
        IEmailSender email,
        IOptions<SmsOptions> smsOptions,
        IOptions<EmailOptions> emailOptions,
        ILogger<AdminNotifier> logger)
    {
        _sms = sms;
        _email = email;
        _smsOptions = smsOptions.Value;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public IReadOnlyList<string> GetAdminPhones() => _smsOptions.GetAdminNotifyPhoneList();

    public IReadOnlyList<string> GetAdminEmails() => _emailOptions.GetAdminNotifyEmailList();

    /// <inheritdoc cref="IAdminSmsNotifier.NotifyAdminsAsync" />
    public Task NotifyAdminsAsync(string text, CancellationToken cancellationToken = default) =>
        SendSmsAsync(text, cancellationToken);

    public async Task NotifyAdminsAsync(string subject, string message, CancellationToken cancellationToken = default)
    {
        await SendSmsAsync(message, cancellationToken);
        await SendEmailsAsync(subject, message, cancellationToken);
    }

    public Task NotifyAdminsByEmailAsync(
        string subject,
        string message,
        CancellationToken cancellationToken = default) =>
        SendEmailsAsync(subject, message, cancellationToken);

    private async Task SendSmsAsync(string text, CancellationToken cancellationToken)
    {
        var phones = GetAdminPhones();
        if (phones.Count == 0)
            return;

        var primary = phones[0];
        var extra = phones.Count > 1 ? phones.Skip(1).ToList() : null;
        var result = await _sms.SendAsync(new SmsMessage(primary, text, extra), cancellationToken);
        if (!result.Succeeded && !result.Skipped)
            _logger.LogWarning("Admin SMS notify failed: {Reason}", result.ProviderMessage);
    }

    private async Task SendEmailsAsync(string subject, string message, CancellationToken cancellationToken)
    {
        var emails = GetAdminEmails();
        if (emails.Count == 0)
            return;

        foreach (var to in emails)
        {
            try
            {
                await _email.SendAsync(
                    new EmailMessage(to, subject, message, IsHtml: false),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admin email notify failed for {Email}", to);
            }
        }
    }
}

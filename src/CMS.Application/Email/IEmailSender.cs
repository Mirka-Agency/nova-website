namespace CMS.Application.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string Body,
    bool IsHtml = false,
    string? ReplyTo = null,
    string? FromDisplayName = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

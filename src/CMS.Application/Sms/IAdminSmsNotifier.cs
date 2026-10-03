namespace CMS.Application.Sms;

public interface IAdminSmsNotifier
{
    IReadOnlyList<string> GetAdminPhones();
    Task NotifyAdminsAsync(string text, CancellationToken cancellationToken = default);
}

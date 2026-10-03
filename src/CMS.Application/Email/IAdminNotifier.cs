namespace CMS.Application.Email;

/// <summary>Operational alerts to site admins (orders, forms, etc.).</summary>
public interface IAdminNotifier
{
    IReadOnlyList<string> GetAdminEmails();
    Task NotifyAdminsAsync(string subject, string message, CancellationToken cancellationToken = default);
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>CSV of admin email addresses for operational alerts.</summary>
    public string AdminNotifyEmails { get; set; } = string.Empty;

    public IReadOnlyList<string> GetAdminNotifyEmailList()
    {
        if (string.IsNullOrWhiteSpace(AdminNotifyEmails))
            return [];

        return AdminNotifyEmails
            .Split([',', ';', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(e => e.Contains('@', StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

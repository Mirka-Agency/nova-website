namespace CMS.Application.Email;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Email:Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = string.Empty;
    public string? FromDisplayName { get; set; }
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Optional MailKit secure mode override: None, Auto, SslOnConnect, StartTls, StartTlsWhenAvailable.
    /// When empty, derived from Port + EnableSsl (465 → SslOnConnect, else StartTls when SSL enabled).
    /// </summary>
    public string? SecureSocketOptions { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);
}

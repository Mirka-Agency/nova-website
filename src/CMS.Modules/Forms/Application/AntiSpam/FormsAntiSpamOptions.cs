using CMS.Application.Security;

namespace CMS.Modules.Forms.Application.AntiSpam;

/// <summary>
/// Legacy forms captcha section (<c>Forms:AntiSpam</c>).
/// Live keys are read from the shared <c>Captcha</c> section; this section is only a fallback.
/// </summary>
public sealed class FormsAntiSpamOptions
{
    public const string SectionName = "Forms:AntiSpam";

    public FormsAntiSpamProviderKeys Turnstile { get; set; } = new();
    public FormsAntiSpamProviderKeys Recaptcha { get; set; } = new();
    public FormsAntiSpamProviderKeys Hcaptcha { get; set; } = new();

    public FormsAntiSpamProviderKeys? ForProvider(string? providerId) =>
        (providerId ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            FormAntiSpamProviderIds.Turnstile => Turnstile,
            FormAntiSpamProviderIds.Recaptcha => Recaptcha,
            FormAntiSpamProviderIds.Hcaptcha => Hcaptcha,
            _ => null
        };
}

public sealed class FormsAntiSpamProviderKeys
{
    public string? SiteKey { get; set; }
    public string? SecretKey { get; set; }

    public bool HasSiteKey => !string.IsNullOrWhiteSpace(SiteKey);
    public bool HasSecretKey => !string.IsNullOrWhiteSpace(SecretKey);
}

public interface IFormAntiSpamCredentials
{
    string? GetSiteKey(string? providerId);
    string? GetSecretKey(string? providerId);
    bool IsConfigured(string? providerId);
}

public sealed class FormAntiSpamCredentials : IFormAntiSpamCredentials
{
    private readonly ISharedCaptchaCredentials _shared;

    public FormAntiSpamCredentials(ISharedCaptchaCredentials shared)
    {
        _shared = shared;
    }

    public string? GetSiteKey(string? providerId) => _shared.GetSiteKey(providerId);

    public string? GetSecretKey(string? providerId) => _shared.GetSecretKey(providerId);

    public bool IsConfigured(string? providerId) => _shared.IsConfigured(providerId);
}

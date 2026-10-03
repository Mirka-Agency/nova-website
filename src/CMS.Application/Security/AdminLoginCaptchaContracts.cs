namespace CMS.Application.Security;

/// <summary>
/// Admin login captcha (Turnstile / reCAPTCHA v3 / hCaptcha).
/// Keys come from the shared <c>Captcha</c> section. This section only selects the provider.
/// </summary>
public sealed class AdminLoginCaptchaOptions
{
    public const string SectionName = "Admin:LoginCaptcha";

    /// <summary>
    /// <c>none</c>, <c>turnstile</c>, <c>recaptcha</c> (Google reCAPTCHA v3), or <c>hcaptcha</c>.
    /// Site and secret keys are shared via <c>Captcha</c>, not stored on this section.
    /// </summary>
    public string Provider { get; set; } = AdminLoginCaptchaProviders.None;

    public AdminLoginCaptchaProviderKeys Turnstile { get; set; } = new();
    public AdminLoginCaptchaProviderKeys Recaptcha { get; set; } = new();
    public AdminLoginCaptchaProviderKeys Hcaptcha { get; set; } = new();

    public string NormalizedProvider => AdminLoginCaptchaProviders.Normalize(Provider);

    public bool IsEnabled => AdminLoginCaptchaProviders.IsExternal(NormalizedProvider);

    public AdminLoginCaptchaProviderKeys? ForProvider(string? providerId) =>
        AdminLoginCaptchaProviders.Normalize(providerId) switch
        {
            AdminLoginCaptchaProviders.Turnstile => Turnstile,
            AdminLoginCaptchaProviders.Recaptcha => Recaptcha,
            AdminLoginCaptchaProviders.Hcaptcha => Hcaptcha,
            _ => null
        };
}

public sealed class AdminLoginCaptchaProviderKeys
{
    public string? SiteKey { get; set; }
    public string? SecretKey { get; set; }

    public bool HasSiteKey => !string.IsNullOrWhiteSpace(SiteKey);
    public bool HasSecretKey => !string.IsNullOrWhiteSpace(SecretKey);
}

public static class AdminLoginCaptchaProviders
{
    public const string None = "none";
    public const string Turnstile = "turnstile";
    public const string Recaptcha = "recaptcha";
    public const string Hcaptcha = "hcaptcha";

    public static readonly IReadOnlyList<string> External =
    [
        Turnstile,
        Recaptcha,
        Hcaptcha
    ];

    public static string Normalize(string? provider)
    {
        var value = (provider ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(value) || value is "off" or "disabled" or "false")
            return None;
        return value;
    }

    public static bool IsExternal(string? provider) =>
        External.Contains(Normalize(provider), StringComparer.Ordinal);
}

public sealed record AdminLoginCaptchaDisplay(
    bool Enabled,
    string Provider,
    string? SiteKey);

public sealed record AdminLoginCaptchaValidationResult(bool Succeeded, string? ErrorMessage)
{
    public static AdminLoginCaptchaValidationResult Ok() => new(true, null);

    public static AdminLoginCaptchaValidationResult Fail(string message) =>
        new(false, message);
}

public interface IAdminLoginCaptchaService
{
    AdminLoginCaptchaDisplay GetDisplay();

    Task<AdminLoginCaptchaValidationResult> ValidateAsync(
        string? captchaToken,
        string? remoteIp,
        CancellationToken cancellationToken = default);
}

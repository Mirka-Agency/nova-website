namespace CMS.Application.Security;

/// <summary>
/// Shared captcha credentials for admin login, forms, and comments.
/// Section: <c>Captcha</c> (env <c>Captcha__*</c>).
/// Legacy <c>Forms:AntiSpam</c>, <c>Comments:AntiSpam</c>, and <c>Admin:LoginCaptcha</c> keys are fallbacks.
/// Google reCAPTCHA is v3 (score), not the v2 checkbox.
/// </summary>
public sealed class SharedCaptchaOptions
{
    public const string SectionName = "Captcha";

    public SharedCaptchaProviderKeys Turnstile { get; set; } = new();
    public SharedCaptchaRecaptchaKeys Recaptcha { get; set; } = new();
    public SharedCaptchaProviderKeys Hcaptcha { get; set; } = new();

    public SharedCaptchaProviderKeys? ForProvider(string? providerId) =>
        SharedCaptchaProviders.Normalize(providerId) switch
        {
            SharedCaptchaProviders.Turnstile => Turnstile,
            SharedCaptchaProviders.Recaptcha => Recaptcha,
            SharedCaptchaProviders.Hcaptcha => Hcaptcha,
            _ => null
        };
}

public class SharedCaptchaProviderKeys
{
    public string? SiteKey { get; set; }
    public string? SecretKey { get; set; }

    public bool HasSiteKey => SharedCaptchaKeys.Normalize(SiteKey) is not null;
    public bool HasSecretKey => SharedCaptchaKeys.Normalize(SecretKey) is not null;
}

public sealed class SharedCaptchaRecaptchaKeys : SharedCaptchaProviderKeys
{
    /// <summary>v3 score threshold (0–1). Values outside that range use <see cref="RecaptchaV3Evaluation.DefaultMinScore"/>.</summary>
    public double MinScore { get; set; } = RecaptchaV3Evaluation.DefaultMinScore;
}

public static class SharedCaptchaProviders
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

    public static string? Pascal(string? provider) => Normalize(provider) switch
    {
        Turnstile => "Turnstile",
        Recaptcha => "Recaptcha",
        Hcaptcha => "Hcaptcha",
        _ => null
    };
}

public static class SharedCaptchaActions
{
    public const string Login = "login";
    public const string Form = "form";
    public const string Comment = "comment";
}

public static class SharedCaptchaKeys
{
    public static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    /// <summary>First non-empty key. Order is the precedence (shared section, then legacy sections).</summary>
    public static string? Coalesce(params string?[] values)
    {
        foreach (var value in values)
        {
            var normalized = Normalize(value);
            if (normalized is not null)
                return normalized;
        }

        return null;
    }
}

public static class RecaptchaV3Evaluation
{
    public const double DefaultMinScore = 0.5;

    /// <summary>Google's public test secret. siteverify succeeds without a v3 score.</summary>
    public const string GoogleTestSecret = "6LeIxAcTAAAAAGG-vFI1TnRWxMZNFuojJ4WifJWe";

    public static double ClampMinScore(double minScore) =>
        minScore is > 0 and <= 1 ? minScore : DefaultMinScore;

    public static bool AllowsMissingScore(string? secret) =>
        string.Equals(NormalizeSecret(secret), GoogleTestSecret, StringComparison.Ordinal);

    public static bool IsAccepted(
        bool success,
        double? score,
        string? action,
        string? expectedAction,
        double minScore,
        bool allowMissingScore)
    {
        if (!success)
            return false;

        if (!string.IsNullOrWhiteSpace(expectedAction)
            && !string.IsNullOrWhiteSpace(action)
            && !string.Equals(action.Trim(), expectedAction.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        if (score is null)
            return allowMissingScore;

        return score.Value >= ClampMinScore(minScore);
    }

    private static string? NormalizeSecret(string? secret)
    {
        var trimmed = secret?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}

public interface ISharedCaptchaCredentials
{
    string? GetSiteKey(string? providerId);
    string? GetSecretKey(string? providerId);
    bool IsConfigured(string? providerId);
    double RecaptchaMinScore { get; }
}

public sealed record SharedCaptchaValidationResult(bool Succeeded, string? ErrorMessage)
{
    public static SharedCaptchaValidationResult Ok() => new(true, null);

    public static SharedCaptchaValidationResult Fail(string message) => new(false, message);
}

public interface ISharedCaptchaVerifier
{
    Task<SharedCaptchaValidationResult> ValidateAsync(
        string? provider,
        string? token,
        string? remoteIp,
        string? expectedAction = null,
        CancellationToken cancellationToken = default);
}

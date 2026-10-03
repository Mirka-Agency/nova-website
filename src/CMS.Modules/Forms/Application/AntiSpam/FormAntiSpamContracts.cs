using CMS.Modules.Forms.Application.Actions;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Entities;

namespace CMS.Modules.Forms.Application.AntiSpam;

public static class FormAntiSpamProviderIds
{
    public const string Honeypot = "honeypot";
    public const string SimpleCaptcha = "simple_captcha";
    public const string Turnstile = "turnstile";
    public const string Recaptcha = "recaptcha";
    public const string Hcaptcha = "hcaptcha";

    public static readonly IReadOnlyList<string> All =
    [
        Honeypot,
        SimpleCaptcha,
        Turnstile,
        Recaptcha,
        Hcaptcha
    ];
}

public sealed class FormAntiSpamValidationRequest
{
    public required FormAntiSpamSchema Settings { get; init; }
    public required FormDefinition Form { get; init; }
    public string? HoneypotValue { get; init; }
    public string? CaptchaAnswer { get; init; }
    public string? ProviderToken { get; init; }
    public IReadOnlyDictionary<string, string?> Values { get; init; }
        = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public string? RemoteIp { get; init; }
}

public sealed class FormAntiSpamValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public Dictionary<string, string[]> Errors { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public static FormAntiSpamValidationResult Ok() => new();

    public static FormAntiSpamValidationResult Fail(string key, string message) =>
        new() { Errors = { [key] = [message] } };
}

public interface IFormAntiSpamProvider
{
    string ProviderId { get; }

    /// <summary>Whether the public UI should render a visible challenge for this provider.</summary>
    bool RequiresVisibleChallenge { get; }

    Task<FormAntiSpamValidationResult> ValidateAsync(
        FormAntiSpamValidationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IFormAntiSpamService
{
    FormAntiSpamSchema ResolveSettings(FormDefinition form, FormSchemaDocument? schema);
    Task<FormAntiSpamValidationResult> ValidateAsync(
        FormAntiSpamValidationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class FormAntiSpamService : IFormAntiSpamService
{
    private readonly IReadOnlyDictionary<string, IFormAntiSpamProvider> _providers;
    private readonly IFormAntiSpamCredentials _credentials;

    public FormAntiSpamService(
        IEnumerable<IFormAntiSpamProvider> providers,
        IFormAntiSpamCredentials credentials)
    {
        _providers = providers.ToDictionary(p => p.ProviderId, StringComparer.OrdinalIgnoreCase);
        _credentials = credentials;
    }

    public FormAntiSpamSchema ResolveSettings(FormDefinition form, FormSchemaDocument? schema)
    {
        _ = form;
        FormAntiSpamSchema settings;
        if (schema?.AntiSpam is { } fromSchema && !string.IsNullOrWhiteSpace(fromSchema.Provider))
            settings = fromSchema;
        else
        {
            settings = new FormAntiSpamSchema
            {
                Enabled = true,
                Provider = FormAntiSpamProviderIds.Honeypot,
                Config = new Dictionary<string, object?>()
            };
        }

        return ApplyGlobalCredentials(settings);
    }

    public async Task<FormAntiSpamValidationResult> ValidateAsync(
        FormAntiSpamValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Settings.Enabled)
            return FormAntiSpamValidationResult.Ok();

        // Global honeypot gate: always reject filled honeypot when anti-spam is enabled.
        if (!string.IsNullOrWhiteSpace(request.HoneypotValue))
            return FormAntiSpamValidationResult.Fail("honeypot", "ارسال رد شد.");

        var providerId = string.IsNullOrWhiteSpace(request.Settings.Provider)
            ? FormAntiSpamProviderIds.Honeypot
            : request.Settings.Provider.Trim();

        if (!_providers.TryGetValue(providerId, out var provider))
            return FormAntiSpamValidationResult.Fail("antispam", $"ارائه‌دهنده ضداسپم ناشناخته: {providerId}");

        return await provider.ValidateAsync(request, cancellationToken);
    }

    private FormAntiSpamSchema ApplyGlobalCredentials(FormAntiSpamSchema settings)
    {
        var provider = string.IsNullOrWhiteSpace(settings.Provider)
            ? FormAntiSpamProviderIds.Honeypot
            : settings.Provider.Trim();

        var siteKey = _credentials.GetSiteKey(provider);
        var secretKey = _credentials.GetSecretKey(provider);
        if (siteKey is null && secretKey is null)
            return settings;

        var config = new Dictionary<string, object?>(
            settings.Config ?? new Dictionary<string, object?>(),
            StringComparer.OrdinalIgnoreCase);

        // Environment / options win over any legacy per-form keys in SchemaJson.
        if (siteKey is not null)
            config["siteKey"] = siteKey;
        if (secretKey is not null)
            config["secretKey"] = secretKey;

        return new FormAntiSpamSchema
        {
            Enabled = settings.Enabled,
            Provider = provider,
            Config = config
        };
    }
}

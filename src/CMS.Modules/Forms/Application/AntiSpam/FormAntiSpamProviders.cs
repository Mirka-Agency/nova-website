using System.Text.Json;
using CMS.Application.Security;
using CMS.Modules.Forms.Application.Actions;
using CMS.Modules.Forms.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CMS.Modules.Forms.Application.AntiSpam;

public sealed class HoneypotAntiSpamProvider : IFormAntiSpamProvider
{
    public string ProviderId => FormAntiSpamProviderIds.Honeypot;
    public bool RequiresVisibleChallenge => false;

    public Task<FormAntiSpamValidationResult> ValidateAsync(
        FormAntiSpamValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Honeypot already enforced in FormAntiSpamService before provider dispatch.
        return Task.FromResult(FormAntiSpamValidationResult.Ok());
    }
}

public sealed class SimpleCaptchaAntiSpamProvider : IFormAntiSpamProvider
{
    public string ProviderId => FormAntiSpamProviderIds.SimpleCaptcha;
    public bool RequiresVisibleChallenge => true;

    public Task<FormAntiSpamValidationResult> ValidateAsync(
        FormAntiSpamValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var captchaFields = request.Form.Fields
            .Where(f => f.FieldType == FormFieldType.Captcha)
            .ToList();

        var answer = request.CaptchaAnswer?.Trim();
        if (string.IsNullOrWhiteSpace(answer))
        {
            var key = captchaFields.FirstOrDefault()?.Key ?? "captcha";
            if (request.Values.TryGetValue(key, out var fromValues) && !string.IsNullOrWhiteSpace(fromValues))
                answer = fromValues.Trim();
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            var key = captchaFields.FirstOrDefault()?.Key ?? "captcha";
            return Task.FromResult(FormAntiSpamValidationResult.Fail(key, "پاسخ کپچا الزامی است."));
        }

        var expectedFromConfig = FormActionConfigReader.GetString(request.Settings.Config, "expected");
        var fieldExpectations = captchaFields
            .Select(f => (Field: f, Expected: TryReadExpectedAnswer(f.SettingsJson)))
            .ToList();

        var hasExpected = !string.IsNullOrWhiteSpace(expectedFromConfig)
            || fieldExpectations.Any(x => !string.IsNullOrWhiteSpace(x.Expected));

        if (!hasExpected)
        {
            return Task.FromResult(FormAntiSpamValidationResult.Fail(
                "captcha",
                "کپچای ساده پیکربندی نشده است. پاسخ مورد انتظار را در تنظیمات فرم مشخص کنید."));
        }

        if (!string.IsNullOrWhiteSpace(expectedFromConfig)
            && !string.Equals(answer, expectedFromConfig, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(FormAntiSpamValidationResult.Fail("captcha", "پاسخ کپچا نادرست است."));
        }

        foreach (var (field, expected) in fieldExpectations)
        {
            if (expected is not null
                && !string.Equals(answer, expected, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(FormAntiSpamValidationResult.Fail(field.Key, "پاسخ کپچا نادرست است."));
            }
        }

        return Task.FromResult(FormAntiSpamValidationResult.Ok());
    }

    private static string? TryReadExpectedAnswer(string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(settingsJson);
            if (doc.RootElement.TryGetProperty("expected", out var expected))
                return expected.GetString()?.Trim();
            if (doc.RootElement.TryGetProperty("answer", out var answer))
                return answer.GetString()?.Trim();
        }
        catch (JsonException)
        {
            return settingsJson.Trim();
        }

        return null;
    }
}

/// <summary>
/// External challenge providers (Turnstile / reCAPTCHA / hCaptcha).
/// Requires secretKey in config and verifies the token via the provider API.
/// </summary>
public sealed class ExternalChallengeAntiSpamProvider : IFormAntiSpamProvider
{
    private readonly string _providerId;
    private readonly string _verifyUrl;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ISharedCaptchaVerifier? _sharedVerifier;
    private readonly string? _expectedAction;

    public ExternalChallengeAntiSpamProvider(
        string providerId,
        string verifyUrl,
        ILogger logger,
        IHttpClientFactory? httpClientFactory = null,
        ISharedCaptchaVerifier? sharedVerifier = null,
        string? expectedAction = null)
    {
        _providerId = providerId;
        _verifyUrl = verifyUrl;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _sharedVerifier = sharedVerifier;
        _expectedAction = expectedAction;
    }

    public string ProviderId => _providerId;

    public bool RequiresVisibleChallenge =>
        !string.Equals(_providerId, FormAntiSpamProviderIds.Recaptcha, StringComparison.OrdinalIgnoreCase);

    public async Task<FormAntiSpamValidationResult> ValidateAsync(
        FormAntiSpamValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = request.ProviderToken?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            return FormAntiSpamValidationResult.Fail("antispam", "تأیید امنیتی الزامی است.");

        if (_sharedVerifier is not null)
        {
            var shared = await _sharedVerifier.ValidateAsync(
                _providerId,
                token,
                request.RemoteIp,
                _expectedAction,
                cancellationToken);
            return shared.Succeeded
                ? FormAntiSpamValidationResult.Ok()
                : FormAntiSpamValidationResult.Fail("antispam", shared.ErrorMessage ?? "تأیید امنیتی ناموفق بود.");
        }

        var secret = FormActionConfigReader.GetString(request.Settings.Config, "secretKey")
                     ?? FormActionConfigReader.GetString(request.Settings.Config, "secret");

        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogWarning(
                "AntiSpam provider {Provider} has no secretKey in env/config; rejecting challenge.",
                _providerId);
            return FormAntiSpamValidationResult.Fail("antispam", "پیکربندی ضداسپم ناقص است (Secret Key در env).");
        }

        if (_httpClientFactory is null)
        {
            _logger.LogWarning(
                "AntiSpam provider {Provider}: HttpClient unavailable; rejecting challenge.",
                _providerId);
            return FormAntiSpamValidationResult.Fail("antispam", "سرویس تأیید امنیتی در دسترس نیست.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("forms-antispam");
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = secret,
                ["response"] = token,
                ["remoteip"] = request.RemoteIp ?? string.Empty
            });
            using var response = await client.PostAsync(_verifyUrl, content, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var success = doc.RootElement.TryGetProperty("success", out var s)
                          && s.ValueKind == JsonValueKind.True;

            return success
                ? FormAntiSpamValidationResult.Ok()
                : FormAntiSpamValidationResult.Fail("antispam", "تأیید امنیتی ناموفق بود.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AntiSpam provider {Provider} verification failed", _providerId);
            return FormAntiSpamValidationResult.Fail("antispam", "خطا در تأیید امنیتی. دوباره تلاش کنید.");
        }
    }
}

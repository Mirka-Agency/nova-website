using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CMS.Application.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Security;

public sealed class SharedCaptchaCredentials : ISharedCaptchaCredentials
{
    private readonly SharedCaptchaOptions _options;
    private readonly IConfiguration _configuration;

    public SharedCaptchaCredentials(
        IOptions<SharedCaptchaOptions> options,
        IConfiguration configuration)
    {
        _options = options.Value;
        _configuration = configuration;
    }

    public double RecaptchaMinScore => RecaptchaV3Evaluation.ClampMinScore(_options.Recaptcha.MinScore);

    public string? GetSiteKey(string? providerId) => Read(providerId, siteKey: true);

    public string? GetSecretKey(string? providerId) => Read(providerId, siteKey: false);

    public bool IsConfigured(string? providerId) =>
        GetSiteKey(providerId) is not null && GetSecretKey(providerId) is not null;

    private string? Read(string? providerId, bool siteKey)
    {
        var pascal = SharedCaptchaProviders.Pascal(providerId);
        if (pascal is null)
            return null;

        var keys = _options.ForProvider(providerId);
        var fromShared = siteKey ? keys?.SiteKey : keys?.SecretKey;
        var keyName = siteKey ? "SiteKey" : "SecretKey";

        return SharedCaptchaKeys.Coalesce(
            fromShared,
            _configuration[$"Forms:AntiSpam:{pascal}:{keyName}"],
            _configuration[$"Comments:AntiSpam:{pascal}:{keyName}"],
            _configuration[$"Admin:LoginCaptcha:{pascal}:{keyName}"]);
    }
}

public sealed class SharedCaptchaVerifier : ISharedCaptchaVerifier
{
    public const string HttpClientName = "SharedCaptcha";

    private readonly ISharedCaptchaCredentials _credentials;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SharedCaptchaVerifier> _logger;

    public SharedCaptchaVerifier(
        ISharedCaptchaCredentials credentials,
        IHttpClientFactory httpClientFactory,
        ILogger<SharedCaptchaVerifier> logger)
    {
        _credentials = credentials;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SharedCaptchaValidationResult> ValidateAsync(
        string? provider,
        string? token,
        string? remoteIp,
        string? expectedAction = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = SharedCaptchaProviders.Normalize(provider);
        if (!SharedCaptchaProviders.IsExternal(normalized))
            return SharedCaptchaValidationResult.Ok();

        if (!_credentials.IsConfigured(normalized))
            return SharedCaptchaValidationResult.Fail("کلیدهای کپچا پیکربندی نشده‌اند.");

        var captchaToken = token?.Trim();
        if (string.IsNullOrWhiteSpace(captchaToken))
            return SharedCaptchaValidationResult.Fail("تأیید امنیتی الزامی است.");

        var endpoint = normalized switch
        {
            SharedCaptchaProviders.Turnstile => "https://challenges.cloudflare.com/turnstile/v0/siteverify",
            SharedCaptchaProviders.Recaptcha => "https://www.google.com/recaptcha/api/siteverify",
            SharedCaptchaProviders.Hcaptcha => "https://hcaptcha.com/siteverify",
            _ => null
        };

        if (endpoint is null)
            return SharedCaptchaValidationResult.Fail("ارائه‌دهنده کپچا نامعتبر است.");

        var secret = _credentials.GetSecretKey(normalized)!;

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var form = new Dictionary<string, string>
            {
                ["secret"] = secret,
                ["response"] = captchaToken
            };
            if (!string.IsNullOrWhiteSpace(remoteIp))
                form["remoteip"] = remoteIp;

            using var content = new FormUrlEncodedContent(form);
            using var response = await client.PostAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>(cancellationToken);

            if (normalized == SharedCaptchaProviders.Recaptcha)
            {
                var accepted = RecaptchaV3Evaluation.IsAccepted(
                    payload?.Success == true,
                    payload?.Score,
                    payload?.Action,
                    expectedAction,
                    _credentials.RecaptchaMinScore,
                    RecaptchaV3Evaluation.AllowsMissingScore(secret));

                if (!accepted)
                {
                    _logger.LogWarning(
                        "reCAPTCHA v3 rejected. Action={Action} Expected={ExpectedAction} Score={Score} Errors={Errors}",
                        payload?.Action,
                        expectedAction,
                        payload?.Score,
                        payload?.ErrorCodes is null ? null : string.Join(",", payload.ErrorCodes));
                    return SharedCaptchaValidationResult.Fail("تأیید امنیتی ناموفق بود.");
                }

                return SharedCaptchaValidationResult.Ok();
            }

            if (payload?.Success == true)
                return SharedCaptchaValidationResult.Ok();

            return SharedCaptchaValidationResult.Fail("تأیید امنیتی ناموفق بود.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Captcha verification failed for {Provider}", normalized);
            return SharedCaptchaValidationResult.Fail("خطا در تأیید امنیتی. دوباره تلاش کنید.");
        }
    }

    private sealed class SiteVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public double? Score { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}

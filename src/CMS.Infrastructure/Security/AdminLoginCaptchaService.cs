using CMS.Application.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Security;

public sealed class AdminLoginCaptchaService : IAdminLoginCaptchaService
{
    private readonly AdminLoginCaptchaOptions _options;
    private readonly ISharedCaptchaCredentials _credentials;
    private readonly ISharedCaptchaVerifier _verifier;
    private readonly ILogger<AdminLoginCaptchaService> _logger;

    public AdminLoginCaptchaService(
        IOptions<AdminLoginCaptchaOptions> options,
        ISharedCaptchaCredentials credentials,
        ISharedCaptchaVerifier verifier,
        ILogger<AdminLoginCaptchaService> logger)
    {
        _options = options.Value;
        _credentials = credentials;
        _verifier = verifier;
        _logger = logger;
    }

    public AdminLoginCaptchaDisplay GetDisplay()
    {
        var provider = _options.NormalizedProvider;
        if (!AdminLoginCaptchaProviders.IsExternal(provider))
            return new AdminLoginCaptchaDisplay(false, AdminLoginCaptchaProviders.None, null);

        return new AdminLoginCaptchaDisplay(true, provider, _credentials.GetSiteKey(provider));
    }

    public async Task<AdminLoginCaptchaValidationResult> ValidateAsync(
        string? captchaToken,
        string? remoteIp,
        CancellationToken cancellationToken = default)
    {
        var provider = _options.NormalizedProvider;
        if (!AdminLoginCaptchaProviders.IsExternal(provider))
            return AdminLoginCaptchaValidationResult.Ok();

        if (!_credentials.IsConfigured(provider))
            return AdminLoginCaptchaValidationResult.Fail("کلیدهای کپچای ورود ادمین پیکربندی نشده‌اند.");

        var expectedAction = string.Equals(provider, AdminLoginCaptchaProviders.Recaptcha, StringComparison.Ordinal)
            ? SharedCaptchaActions.Login
            : null;

        var result = await _verifier.ValidateAsync(
            provider,
            captchaToken,
            remoteIp,
            expectedAction,
            cancellationToken);

        if (!result.Succeeded)
            _logger.LogWarning("Admin login captcha rejected for provider {Provider}", provider);

        return result.Succeeded
            ? AdminLoginCaptchaValidationResult.Ok()
            : AdminLoginCaptchaValidationResult.Fail(result.ErrorMessage ?? "تأیید امنیتی ناموفق بود.");
    }
}

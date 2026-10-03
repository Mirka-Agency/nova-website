using CMS.Application.Security;
using CMS.Modules.Comments.Domain.Enums;

namespace CMS.Modules.Comments.Application.AntiSpam;

public sealed class CommentsAntiSpamOptions
{
    public const string SectionName = "Comments:AntiSpam";

    public CommentsAntiSpamProviderKeys Turnstile { get; set; } = new();
    public CommentsAntiSpamProviderKeys Recaptcha { get; set; } = new();
    public CommentsAntiSpamProviderKeys Hcaptcha { get; set; } = new();

    public CommentsAntiSpamProviderKeys? ForProvider(CommentCaptchaProvider provider) =>
        provider switch
        {
            CommentCaptchaProvider.Turnstile => Turnstile,
            CommentCaptchaProvider.Recaptcha => Recaptcha,
            CommentCaptchaProvider.Hcaptcha => Hcaptcha,
            _ => null
        };
}

public sealed class CommentsAntiSpamProviderKeys
{
    public string? SiteKey { get; set; }
    public string? SecretKey { get; set; }

    public bool HasSiteKey => !string.IsNullOrWhiteSpace(SiteKey);
    public bool HasSecretKey => !string.IsNullOrWhiteSpace(SecretKey);
}

public interface ICommentsAntiSpamCredentials
{
    string? GetSiteKey(CommentCaptchaProvider provider);
    string? GetSecretKey(CommentCaptchaProvider provider);
    bool IsConfigured(CommentCaptchaProvider provider);
}

public sealed class CommentsAntiSpamCredentials : ICommentsAntiSpamCredentials
{
    private readonly ISharedCaptchaCredentials _shared;

    public CommentsAntiSpamCredentials(ISharedCaptchaCredentials shared)
    {
        _shared = shared;
    }

    public string? GetSiteKey(CommentCaptchaProvider provider) =>
        _shared.GetSiteKey(ToProviderId(provider));

    public string? GetSecretKey(CommentCaptchaProvider provider) =>
        _shared.GetSecretKey(ToProviderId(provider));

    public bool IsConfigured(CommentCaptchaProvider provider) =>
        _shared.IsConfigured(ToProviderId(provider));

    private static string? ToProviderId(CommentCaptchaProvider provider) => provider switch
    {
        CommentCaptchaProvider.Turnstile => SharedCaptchaProviders.Turnstile,
        CommentCaptchaProvider.Recaptcha => SharedCaptchaProviders.Recaptcha,
        CommentCaptchaProvider.Hcaptcha => SharedCaptchaProviders.Hcaptcha,
        _ => null
    };
}

public sealed record CommentCaptchaValidationRequest(
    CommentCaptchaProvider Provider,
    string? CaptchaToken,
    string? RemoteIp);

public sealed record CommentCaptchaValidationResult(bool Succeeded, string? ErrorMessage)
{
    public static CommentCaptchaValidationResult Ok() => new(true, null);
    public static CommentCaptchaValidationResult Fail(string message) => new(false, message);
}

public interface ICommentCaptchaValidator
{
    Task<CommentCaptchaValidationResult> ValidateAsync(
        CommentCaptchaValidationRequest request,
        CancellationToken cancellationToken = default);
}

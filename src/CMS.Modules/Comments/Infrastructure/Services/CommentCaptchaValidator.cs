using CMS.Application.Security;
using CMS.Modules.Comments.Application.AntiSpam;
using CMS.Modules.Comments.Domain.Enums;

namespace CMS.Modules.Comments.Infrastructure.Services;

public sealed class CommentCaptchaValidator : ICommentCaptchaValidator
{
    private readonly ISharedCaptchaVerifier _verifier;

    public CommentCaptchaValidator(ISharedCaptchaVerifier verifier)
    {
        _verifier = verifier;
    }

    public async Task<CommentCaptchaValidationResult> ValidateAsync(
        CommentCaptchaValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var providerId = ToProviderId(request.Provider);
        if (providerId is null)
            return CommentCaptchaValidationResult.Ok();

        var expectedAction = request.Provider == CommentCaptchaProvider.Recaptcha
            ? SharedCaptchaActions.Comment
            : null;

        var result = await _verifier.ValidateAsync(
            providerId,
            request.CaptchaToken,
            request.RemoteIp,
            expectedAction,
            cancellationToken);

        return result.Succeeded
            ? CommentCaptchaValidationResult.Ok()
            : CommentCaptchaValidationResult.Fail(result.ErrorMessage ?? "تأیید کپچا ناموفق بود.");
    }

    private static string? ToProviderId(CommentCaptchaProvider provider) => provider switch
    {
        CommentCaptchaProvider.Turnstile => SharedCaptchaProviders.Turnstile,
        CommentCaptchaProvider.Recaptcha => SharedCaptchaProviders.Recaptcha,
        CommentCaptchaProvider.Hcaptcha => SharedCaptchaProviders.Hcaptcha,
        _ => null
    };
}

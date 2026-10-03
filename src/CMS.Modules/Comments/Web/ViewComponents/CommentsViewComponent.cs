using CMS.Application.Common.Features;
using CMS.Modules.Comments.Application.AntiSpam;
using CMS.Modules.Comments.Application.Interfaces;
using CMS.Modules.Comments.Domain.Enums;
using CMS.Modules.Comments.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using System.Security.Claims;

namespace CMS.Modules.Comments.Web.ViewComponents;

public sealed class CommentsViewComponent : ViewComponent
{
    private readonly ICommentService _comments;
    private readonly ICommentSettingsService _settings;
    private readonly ICommentsAntiSpamCredentials _credentials;
    private readonly IFeatureManager _features;

    public CommentsViewComponent(
        ICommentService comments,
        ICommentSettingsService settings,
        ICommentsAntiSpamCredentials credentials,
        IFeatureManager features)
    {
        _comments = comments;
        _settings = settings;
        _credentials = credentials;
        _features = features;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        CommentTargetType targetType,
        Guid targetId,
        string targetTitle,
        CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return Content(string.Empty);

        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.IsEnabledFor(targetType))
            return Content(string.Empty);

        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAuthenticated = !string.IsNullOrWhiteSpace(userId);
        var requiresLogin = !settings.AllowAnonymous && !isAuthenticated;

        var comments = await _comments.ListApprovedForTargetAsync(targetType, targetId, cancellationToken);
        var returnUrl = $"{HttpContext.Request.Path}{HttpContext.Request.QueryString}";

        string? siteKey = null;
        if (settings.EnableCaptcha
            && settings.CaptchaProvider is CommentCaptchaProvider.Turnstile
                or CommentCaptchaProvider.Recaptcha
                or CommentCaptchaProvider.Hcaptcha)
        {
            siteKey = _credentials.GetSiteKey(settings.CaptchaProvider);
        }

        var model = new CommentsSectionViewModel
        {
            TargetType = targetType,
            TargetId = targetId,
            TargetTitle = targetTitle,
            Settings = settings,
            Comments = comments,
            CanSubmit = !requiresLogin,
            RequiresLogin = requiresLogin,
            CaptchaSiteKey = siteKey,
            ReturnUrl = returnUrl,
            Form = new SubmitCommentFormViewModel
            {
                TargetType = targetType,
                TargetId = targetId,
                TargetTitle = targetTitle,
                ReturnUrl = returnUrl,
                AuthorName = isAuthenticated
                    ? (HttpContext.User.Identity?.Name ?? string.Empty)
                    : string.Empty
            }
        };

        return View(model);
    }
}

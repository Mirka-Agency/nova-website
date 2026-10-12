using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Comments.Application.AntiSpam;
using CMS.Modules.Comments.Application.Comments;
using CMS.Modules.Comments.Application.Interfaces;
using CMS.Modules.Comments.Domain.Enums;
using CMS.Modules.Comments.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;
using System.Security.Claims;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Comments.Web.Controllers;

public class PublicCommentsController : Controller
{
    private readonly ICommentService _comments;
    private readonly ICommentSettingsService _settings;
    private readonly ICommentsAntiSpamCredentials _credentials;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<CommentsPublic> _localizer;

    public PublicCommentsController(
        ICommentService comments,
        ICommentSettingsService settings,
        ICommentsAntiSpamCredentials credentials,
        IFeatureManager features,
        IStringLocalizer<CommentsPublic> localizer)
    {
        _comments = comments;
        _settings = settings;
        _credentials = credentials;
        _features = features;
        _localizer = localizer;
    }

    /// <summary>HTML fragment for comment list + "show more" (used by progressive load-more).</summary>
    [HttpGet("/comments/feed")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Feed(
        CommentTargetType targetType,
        Guid targetId,
        string? targetTitle,
        int commentPage = 1,
        CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        if (targetId == Guid.Empty)
            return BadRequest();

        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.IsEnabledFor(targetType))
            return NotFound();

        var page = commentPage < 1 ? 1 : commentPage;
        var paged = await _comments.ListApprovedForTargetPagedAsync(
            targetType,
            targetId,
            page,
            CommentListQuery.PublicPageSize,
            cancellationToken);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAuthenticated = !string.IsNullOrWhiteSpace(userId);
        var requiresLogin = !settings.AllowAnonymous && !isAuthenticated;
        var returnUrl = $"{Request.Headers.Referer}";

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
            TargetTitle = targetTitle ?? string.Empty,
            Settings = settings,
            Comments = paged.Items,
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages,
            CanSubmit = !requiresLogin,
            RequiresLogin = requiresLogin,
            CaptchaSiteKey = siteKey,
            ReturnUrl = returnUrl,
            Form = new SubmitCommentFormViewModel
            {
                TargetType = targetType,
                TargetId = targetId,
                TargetTitle = targetTitle ?? string.Empty,
                ReturnUrl = returnUrl
            }
        };

        return PartialView("Components/Comments/_CommentsFeed", model);
    }

    [HttpPost("/comments")]
    public async Task<IActionResult> Submit(SubmitCommentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        var returnUrl = string.IsNullOrWhiteSpace(model.ReturnUrl) ? "/" : model.ReturnUrl;

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var captchaToken = FirstNonEmpty(
                model.CaptchaToken,
                Request.Form["g-recaptcha-response"],
                Request.Form["h-captcha-response"],
                Request.Form["cf-turnstile-response"]);

            await _comments.SubmitAsync(new SubmitCommentCommand(
                model.TargetType,
                model.TargetId,
                model.TargetTitle,
                userId,
                model.AuthorName,
                model.AuthorEmail,
                model.AuthorPhone,
                model.Body,
                captchaToken,
                HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);

            TempData["CommentSuccess"] = _localizer["CommentSubmitted"].Value;
        }
        catch (DomainValidationException ex)
        {
            TempData["CommentError"] = string.Join(" ", ex.Errors.SelectMany(e => e.Value));
        }
        catch (DomainException ex)
        {
            TempData["CommentError"] = ex.Message;
        }

        return LocalRedirect(returnUrl);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
}

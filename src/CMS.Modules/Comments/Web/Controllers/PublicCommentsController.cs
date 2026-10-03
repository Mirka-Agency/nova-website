using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Comments.Application.Comments;
using CMS.Modules.Comments.Application.Interfaces;
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
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<CommentsPublic> _localizer;

    public PublicCommentsController(
        ICommentService comments,
        IFeatureManager features,
        IStringLocalizer<CommentsPublic> localizer)
    {
        _comments = comments;
        _features = features;
        _localizer = localizer;
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

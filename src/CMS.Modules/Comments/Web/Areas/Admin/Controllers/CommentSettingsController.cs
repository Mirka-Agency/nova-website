using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Comments.Application.Interfaces;
using CMS.Modules.Comments.Application.Settings;
using CMS.Modules.Comments.Domain.Enums;
using CMS.Modules.Comments.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Comments.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewComments")]
public class CommentSettingsController : Controller
{
    private readonly ICommentSettingsService _settings;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<CommentsAdmin> _localizer;

    public CommentSettingsController(
        ICommentSettingsService settings,
        IFeatureManager features,
        IStringLocalizer<CommentsAdmin> localizer)
    {
        _settings = settings;
        _features = features;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        ViewData["Title"] = _localizer["CommentSettings"].Value;
        var dto = await _settings.GetAsync(cancellationToken);
        ViewBag.CaptchaProviders = BuildCaptchaProviders(dto.CaptchaProvider);
        return View(Map(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Index(CommentSettingsFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        ViewData["Title"] = _localizer["CommentSettings"].Value;
        ViewBag.CaptchaProviders = BuildCaptchaProviders(model.CaptchaProvider);

        try
        {
            await _settings.UpdateAsync(new UpdateCommentSettingsCommand(
                model.AllowAnonymous,
                model.EnableOnBlog,
                model.EnableOnEvents,
                model.EnableOnProducts,
                model.EnableOnProductCategories,
                model.ShowEmail,
                model.RequireEmail,
                model.ShowPhone,
                model.RequirePhone,
                model.EnableCaptcha,
                model.CaptchaProvider), cancellationToken);

            TempData["Success"] = _localizer["SettingsSaved"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private IEnumerable<SelectListItem> BuildCaptchaProviders(CommentCaptchaProvider selected) =>
    [
        new SelectListItem(_localizer["CaptchaNone"].Value, ((int)CommentCaptchaProvider.None).ToString(), selected == CommentCaptchaProvider.None),
        new SelectListItem(_localizer["CaptchaTurnstile"].Value, ((int)CommentCaptchaProvider.Turnstile).ToString(), selected == CommentCaptchaProvider.Turnstile),
        new SelectListItem(_localizer["CaptchaRecaptcha"].Value, ((int)CommentCaptchaProvider.Recaptcha).ToString(), selected == CommentCaptchaProvider.Recaptcha),
        new SelectListItem(_localizer["CaptchaHcaptcha"].Value, ((int)CommentCaptchaProvider.Hcaptcha).ToString(), selected == CommentCaptchaProvider.Hcaptcha)
    ];

    private static CommentSettingsFormViewModel Map(CommentSettingsDto dto) =>
        new()
        {
            AllowAnonymous = dto.AllowAnonymous,
            EnableOnBlog = dto.EnableOnBlog,
            EnableOnEvents = dto.EnableOnEvents,
            EnableOnProducts = dto.EnableOnProducts,
            EnableOnProductCategories = dto.EnableOnProductCategories,
            ShowEmail = dto.ShowEmail,
            RequireEmail = dto.RequireEmail,
            ShowPhone = dto.ShowPhone,
            RequirePhone = dto.RequirePhone,
            EnableCaptcha = dto.EnableCaptcha,
            CaptchaProvider = dto.CaptchaProvider
        };
}

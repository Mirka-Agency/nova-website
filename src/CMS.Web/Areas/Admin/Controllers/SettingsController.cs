using CMS.Application.Audit;
using CMS.Application.Settings;
using CMS.Domain.Exceptions;
using CMS.Infrastructure.Auth;
using CMS.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public class SettingsController : Controller
{
    private readonly ISiteSettingsService _settings;
    private readonly IAuditLogger _audit;
    private readonly IStringLocalizer<AdminShared> _localizer;

    public SettingsController(
        ISiteSettingsService settings,
        IAuditLogger audit,
        IStringLocalizer<AdminShared> localizer)
    {
        _settings = settings;
        _audit = audit;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = _localizer["SiteSettings"].Value;
        var dto = await _settings.GetAsync(cancellationToken);
        return View(ToViewModel(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Index(SiteSettingsFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = _localizer["SiteSettings"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _settings.UpdateAsync(
                new UpdateSiteSettingsCommand(
                    model.SiteName,
                    model.Tagline,
                    model.ContactEmail,
                    model.ContactPhone,
                    model.Address,
                    model.FooterText,
                    NullIfWhiteSpace(model.LogoUrl),
                    NullIfWhiteSpace(model.FaviconUrl),
                    model.MetaTitle,
                    model.MetaDescription,
                    NullIfWhiteSpace(model.DefaultOgImageUrl),
                    model.InstagramUrl,
                    model.TelegramUrl,
                    model.TwitterUrl,
                    model.LinkedInUrl,
                    model.AparatUrl,
                    model.FacebookUrl,
                    model.YouTubeUrl,
                    model.WhatsAppUrl,
                    model.MaintenanceMode,
                    model.MaintenanceMessage),
                cancellationToken);
            await _audit.LogAsync("Update", "SiteSettings", details: model.SiteName, cancellationToken: cancellationToken);
            TempData["Success"] = _localizer["SiteSettingsUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException ex)
        {
            foreach (var (key, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(key, message);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SiteSettingsFormViewModel ToViewModel(SiteSettingsDto dto) =>
        new()
        {
            SiteName = dto.SiteName,
            Tagline = dto.Tagline,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            Address = dto.Address,
            FooterText = dto.FooterText,
            LogoUrl = dto.LogoUrl,
            FaviconUrl = dto.FaviconUrl,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            DefaultOgImageUrl = dto.DefaultOgImageUrl,
            InstagramUrl = dto.InstagramUrl,
            TelegramUrl = dto.TelegramUrl,
            TwitterUrl = dto.TwitterUrl,
            LinkedInUrl = dto.LinkedInUrl,
            AparatUrl = dto.AparatUrl,
            FacebookUrl = dto.FacebookUrl,
            YouTubeUrl = dto.YouTubeUrl,
            WhatsAppUrl = dto.WhatsAppUrl,
            MaintenanceMode = dto.MaintenanceMode,
            MaintenanceMessage = dto.MaintenanceMessage
        };
}

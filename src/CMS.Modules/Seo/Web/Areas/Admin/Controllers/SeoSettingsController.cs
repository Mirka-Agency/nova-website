using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Application.Settings;
using CMS.Modules.Seo.Domain.Enums;
using CMS.Modules.Seo.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Seo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewSeo")]
public class SeoSettingsController : Controller
{
    private readonly ISeoSiteSettingsService _settings;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<SeoAdmin> _localizer;

    public SeoSettingsController(
        ISeoSiteSettingsService settings,
        IFeatureManager features,
        IStringLocalizer<SeoAdmin> localizer)
    {
        _settings = settings;
        _features = features;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        ViewData["Title"] = _localizer["SeoSettings"].Value;
        var dto = await _settings.GetAsync(cancellationToken);
        return View(ToViewModel(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Index(SeoSettingsViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        ViewData["Title"] = _localizer["SeoSettings"].Value;
        model.SchemaTypeOptions = BuildSchemaOptions(model.DefaultSchemaType);

        try
        {
            await _settings.UpdateAsync(new UpdateSeoSiteSettingsCommand(
                model.OrganizationName,
                model.OrganizationUrl,
                model.OrganizationLogoUrl,
                model.DefaultSchemaType,
                model.RobotsTxt,
                model.TwitterSiteHandle,
                model.EnableBrokenLinkChecks,
                model.SitemapEnabled), cancellationToken);

            TempData["Success"] = _localizer["SettingsUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            foreach (var (key, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(key, message);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return View(model);
    }

    private SeoSettingsViewModel ToViewModel(SeoSiteSettingsDto dto) =>
        new()
        {
            OrganizationName = dto.OrganizationName,
            OrganizationUrl = dto.OrganizationUrl,
            OrganizationLogoUrl = dto.OrganizationLogoUrl,
            DefaultSchemaType = dto.DefaultSchemaType,
            RobotsTxt = dto.RobotsTxt,
            TwitterSiteHandle = dto.TwitterSiteHandle,
            EnableBrokenLinkChecks = dto.EnableBrokenLinkChecks,
            SitemapEnabled = dto.SitemapEnabled,
            SchemaTypeOptions = BuildSchemaOptions(dto.DefaultSchemaType)
        };

    private List<SelectListItem> BuildSchemaOptions(string? selected)
    {
        var items = new List<SelectListItem>
        {
            new(_localizer["SchemaTypeNone"].Value, string.Empty, string.IsNullOrWhiteSpace(selected))
        };
        foreach (var type in SeoSchemaTypes.Known.OrderBy(x => x))
        {
            items.Add(new SelectListItem(type, type, string.Equals(type, selected, StringComparison.OrdinalIgnoreCase)));
        }

        return items;
    }
}

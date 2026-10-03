using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Modules.Services.Application.Interfaces;
using CMS.Modules.Services.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Services.Web.Controllers;

[Route("Services")]
public class ServicesController : Controller
{
    private readonly IPublicServiceItemQuery _items;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ServicesPublic> _localizer;

    public ServicesController(
        IPublicServiceItemQuery items,
        IFeatureManager features,
        IStringLocalizer<ServicesPublic> localizer)
    {
        _items = items;
        _features = features;
        _localizer = localizer;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["page"])]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Services))
            return NotFound();

        ViewData["Title"] = _localizer["Services"].Value;
        ViewData["NavActive"] = "services";
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Manage("ServiceItems", "مدیریت خدمات", "ViewServices");
        // Archive page matches the Nova template (no compact pager UI).
        var result = await _items.ListPublishedPagedAsync(page, 48, cancellationToken);
        ViewBag.Page = result.Page;
        ViewBag.PageSize = result.PageSize;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.TotalPages = result.TotalPages;
        return View(result.Items);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Services))
            return NotFound();

        var item = await _items.GetPublishedBySlugAsync(slug, cancellationToken);
        if (item is null)
            return NotFound();

        ViewData["Title"] = item.Title;
        ViewData["NavActive"] = "services";
        ViewData["MetaTitle"] = FirstNonEmpty(item.MetaTitle, item.Title);
        ViewData["MetaDescription"] = FirstNonEmpty(item.MetaDescription, item.Excerpt);
        ViewData["MetaKeywords"] = item.SeoKeywords;
        ViewData["CanonicalUrl"] = item.CanonicalUrl;
        ViewData["OgTitle"] = FirstNonEmpty(item.OgTitle, item.MetaTitle, item.Title);
        ViewData["OgDescription"] = FirstNonEmpty(item.OgDescription, item.MetaDescription, item.Excerpt);
        ViewData["OgImage"] = FirstNonEmpty(item.OgImageUrl, item.CoverImageUrl);
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("ServiceItems", item.Id, "ویرایش خدمت", "ManageServices");

        var related = await _items.ListPublishedPagedAsync(1, 8, cancellationToken);
        ViewBag.Related = related.Items
            .Where(x => !string.Equals(x.Slug, item.Slug, StringComparison.OrdinalIgnoreCase))
            .Take(4)
            .ToList();

        return View(item);
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

using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Modules.Video.Application.Interfaces;
using CMS.Modules.Video.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Video.Web.Controllers;

[Route("Videos")]
public class VideosController : Controller
{
    private readonly IPublicVideoItemQuery _items;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<VideoPublic> _localizer;

    public VideosController(
        IPublicVideoItemQuery items,
        IFeatureManager features,
        IStringLocalizer<VideoPublic> localizer)
    {
        _items = items;
        _features = features;
        _localizer = localizer;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["page"])]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Video))
            return NotFound();

        ViewData["Title"] = _localizer["Videos"].Value;
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Manage("VideoItems", "مدیریت ویدیوها", "ViewVideo");
        var result = await _items.ListPublishedPagedAsync(page, 12, cancellationToken);
        ViewBag.Page = result.Page;
        ViewBag.PageSize = result.PageSize;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.TotalPages = result.TotalPages;
        return View(result.Items);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Video))
            return NotFound();

        var item = await _items.GetPublishedBySlugAsync(slug, cancellationToken);
        if (item is null)
            return NotFound();

        ViewData["Title"] = item.Title;
        ViewData["MetaTitle"] = FirstNonEmpty(item.MetaTitle, item.Title);
        ViewData["MetaDescription"] = FirstNonEmpty(item.MetaDescription, item.Excerpt);
        ViewData["MetaKeywords"] = item.SeoKeywords;
        ViewData["CanonicalUrl"] = item.CanonicalUrl;
        ViewData["OgTitle"] = FirstNonEmpty(item.OgTitle, item.MetaTitle, item.Title);
        ViewData["OgDescription"] = FirstNonEmpty(item.OgDescription, item.MetaDescription, item.Excerpt);
        ViewData["OgImage"] = FirstNonEmpty(item.OgImageUrl, item.CoverImageUrl);
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("VideoItems", item.Id, "ویرایش ویدیو", "ManageVideo");
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

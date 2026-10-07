using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Modules.Video.Application.Interfaces;
using CMS.Modules.Video.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Video.Web.Controllers;

[Route("videos")]
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
    [ResponseCache(Duration = 90, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["page"])]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Video))
            return NotFound();

        ViewData["Title"] = _localizer["Videos"].Value;
        ViewData["NavActive"] = "videos";
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Manage("VideoItems", "مدیریت ویدیوها", "ViewVideo");
        // Archive matches Nova template grid/filter UI (client-side paging in main.js).
        var result = await _items.ListPublishedPagedAsync(page, 48, cancellationToken);
        var categories = await _items.ListPublishedCategoriesAsync(cancellationToken);
        ViewBag.Page = result.Page;
        ViewBag.PageSize = result.PageSize;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.TotalPages = result.TotalPages;
        ViewBag.Categories = categories;
        return View(result.Items);
    }

    [HttpGet("{slug}")]
    [ResponseCache(Duration = 90, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Video))
            return NotFound();

        var item = await _items.GetPublishedBySlugAsync(slug, cancellationToken);
        if (item is null)
            return NotFound();

        ViewData["Title"] = item.Title;
        ViewData["NavActive"] = "videos";
        ViewData["MetaTitle"] = FirstNonEmpty(item.MetaTitle, item.Title);
        ViewData["MetaDescription"] = FirstNonEmpty(item.MetaDescription, item.Excerpt);
        ViewData["MetaKeywords"] = item.SeoKeywords;
        ViewData["CanonicalUrl"] = AbsoluteUrl(item.CanonicalUrl, $"/videos/{item.Slug}");
        ViewData["OgTitle"] = FirstNonEmpty(item.OgTitle, item.MetaTitle, item.Title);
        ViewData["OgDescription"] = FirstNonEmpty(item.OgDescription, item.MetaDescription, item.Excerpt);
        ViewData["OgImage"] = FirstNonEmpty(item.OgImageUrl, item.CoverImageUrl);
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("VideoItems", item.Id, "ویرایش ویدیو", "ManageVideo");

        var relatedPage = await _items.ListPublishedPagedAsync(1, 8, cancellationToken);
        ViewBag.Related = relatedPage.Items
            .Where(x => !string.Equals(x.Slug, item.Slug, StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        return View(item);
    }

    private string AbsoluteUrl(string? canonical, string relativePath)
    {
        if (!string.IsNullOrWhiteSpace(canonical))
        {
            var value = canonical.Trim();
            if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return value;

            if (!value.StartsWith('/'))
                value = "/" + value;

            if (Request is null)
                return value;
            return $"{Request.Scheme}://{Request.Host.Value}{value}";
        }

        if (Request is null)
            return relativePath;
        return $"{Request.Scheme}://{Request.Host.Value}{relativePath}";
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

using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Application.Seo;
using CMS.Modules.Services.Application.Interfaces;
using CMS.Modules.Services.Web;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Services.Web.Controllers;

[Route("services")]
public class ServicesController : Controller
{
    private readonly IPublicServiceItemQuery _items;
    private readonly IFeatureManager _features;
    private readonly ISeoDocumentService _seoDocuments;
    private readonly IStringLocalizer<ServicesPublic> _localizer;

    public ServicesController(
        IPublicServiceItemQuery items,
        IFeatureManager features,
        ISeoDocumentService seoDocuments,
        IStringLocalizer<ServicesPublic> localizer)
    {
        _items = items;
        _features = features;
        _seoDocuments = seoDocuments;
        _localizer = localizer;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 90, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["page"])]
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
    [ResponseCache(Duration = 90, Location = ResponseCacheLocation.Any)]
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
        ViewData["CanonicalUrl"] = AbsoluteUrl(item.CanonicalUrl, $"/services/{item.Slug}");
        ViewData["OgTitle"] = FirstNonEmpty(item.OgTitle, item.MetaTitle, item.Title);
        ViewData["OgDescription"] = FirstNonEmpty(item.OgDescription, item.MetaDescription, item.Excerpt);
        ViewData["OgImage"] = FirstNonEmpty(item.OgImageUrl, item.CoverImageUrl);
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("ServiceItems", item.Id, "ویرایش خدمت", "ManageServices");

        var related = await _items.ListPublishedPagedAsync(1, 8, cancellationToken);
        ViewBag.Related = related.Items
            .Where(x => !string.Equals(x.Slug, item.Slug, StringComparison.OrdinalIgnoreCase))
            .Take(4)
            .ToList();

        if (await _features.IsEnabledAsync(FeatureNames.Seo))
        {
            var seo = await _seoDocuments.GetAsync(SeoContentTypeKeys.ServiceItem, item.Id, cancellationToken);
            if (seo is not null)
            {
                ViewData["Robots"] = SeoEditorHelper.FormatRobotsMeta(seo.RobotsIndex, seo.RobotsFollow);
                ViewData["SchemaJson"] = FirstNonEmpty(
                    seo.SchemaJson,
                    SeoSchemaBuilder.BuildContentSchema(
                        seo.SchemaType ?? "Service",
                        item.Title,
                        FirstNonEmpty(item.MetaDescription, item.Excerpt),
                        AbsoluteUrl(item.CanonicalUrl, $"/services/{item.Slug}"),
                        FirstNonEmpty(item.OgImageUrl, item.CoverImageUrl),
                        item.AuthorDisplayName,
                        item.PublishedAtUtc,
                        null));
            }
        }

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

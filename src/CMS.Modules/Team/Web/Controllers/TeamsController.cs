using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Application.Seo;
using CMS.Modules.Team.Application.Interfaces;
using CMS.Modules.Team.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Team.Web.Controllers;

[Route("doctors")]
public class TeamsController : Controller
{
    private readonly IPublicTeamItemQuery _items;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<TeamPublic> _localizer;

    public TeamsController(
        IPublicTeamItemQuery items,
        IFeatureManager features,
        IStringLocalizer<TeamPublic> localizer)
    {
        _items = items;
        _features = features;
        _localizer = localizer;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 90, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["page"])]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        ViewData["Title"] = "متخصصان کلینیک";
        ViewData["NavActive"] = "doctors";
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Manage("TeamItems", "مدیریت تیم", "ViewTeam");
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
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        var item = await _items.GetPublishedBySlugAsync(slug, cancellationToken);
        if (item is null)
            return NotFound();

        ViewData["Title"] = item.Title;
        ViewData["NavActive"] = "doctors";
        ViewData["MetaTitle"] = FirstNonEmpty(item.MetaTitle, item.Title);
        ViewData["MetaDescription"] = FirstNonEmpty(item.MetaDescription, item.Excerpt);
        ViewData["MetaKeywords"] = item.SeoKeywords;
        ViewData["CanonicalUrl"] = AbsoluteUrl(item.CanonicalUrl, $"/doctors/{item.Slug}");
        ViewData["OgTitle"] = FirstNonEmpty(item.OgTitle, item.MetaTitle, item.Title);
        ViewData["OgDescription"] = FirstNonEmpty(item.OgDescription, item.MetaDescription, item.Excerpt);
        ViewData["OgImage"] = FirstNonEmpty(item.OgImageUrl, item.CoverImageUrl);
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("TeamItems", item.Id, "ویرایش عضو تیم", "ManageTeam");

        var related = await _items.ListPublishedPagedAsync(1, 8, cancellationToken);
        ViewBag.Related = related.Items
            .Where(x => !string.Equals(x.Slug, item.Slug, StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        return View(item);
    }

    private string AbsoluteUrl(string? canonical, string relativePath) =>
        CanonicalUrlBuilder.ForContent(canonical, relativePath, Request.Scheme, Request.Host.Value ?? "localhost");

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

using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Modules.Team.Application.Interfaces;
using CMS.Modules.Team.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Team.Web.Controllers;

[Route("Teams")]
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
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["page"])]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        ViewData["Title"] = _localizer["Teams"].Value;
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Manage("TeamItems", "مدیریت تیم", "ViewTeam");
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
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
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
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("TeamItems", item.Id, "ویرایش عضو تیم", "ManageTeam");
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

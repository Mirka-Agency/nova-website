using CMS.Application.Common.Features;
using CMS.Modules.Team.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Team.Web.Controllers;

/// <summary>Permanent redirects from the old /teams (and /Teams via lowercase middleware) URLs to /doctors.</summary>
[Route("teams")]
public class TeamsLegacyRedirectController : Controller
{
    private readonly IPublicTeamItemQuery _items;
    private readonly IFeatureManager _features;

    public TeamsLegacyRedirectController(IPublicTeamItemQuery items, IFeatureManager features)
    {
        _items = items;
        _features = features;
    }

    [HttpGet("")]
    public IActionResult Index() => RedirectPermanent("/doctors/");

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        var normalized = slug.Trim().Trim('/').ToLowerInvariant();
        var item = await _items.GetPublishedBySlugAsync(normalized, cancellationToken);
        if (item is null)
            return NotFound();

        return RedirectPermanent($"/doctors/{item.Slug}/");
    }
}

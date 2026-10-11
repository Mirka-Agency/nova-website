using CMS.Application.Common.Features;
using CMS.Modules.News.Application.Interfaces;
using CMS.Modules.News.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Modules.News.Web.Controllers;

/// <summary>Permanent redirects from the old /events URLs to /event.</summary>
[Route("events")]
public class EventsLegacyRedirectController : Controller
{
    private readonly IPublicArticleQuery _articles;
    private readonly IFeatureManager _features;

    public EventsLegacyRedirectController(IPublicArticleQuery articles, IFeatureManager features)
    {
        _articles = articles;
        _features = features;
    }

    [HttpGet("")]
    public IActionResult Index() => RedirectPermanent("/event/");

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        var normalized = slug.Trim().Trim('/').ToLowerInvariant();
        var post = await _articles.GetPublishedBySlugAsync(normalized, cancellationToken);
        if (post is null || post.Kind != ArticleKind.Event)
            return NotFound();

        return RedirectPermanent($"/event/{post.Slug}/");
    }
}

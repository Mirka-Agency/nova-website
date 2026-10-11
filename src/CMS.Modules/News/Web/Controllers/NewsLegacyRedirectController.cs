using CMS.Application.Common.Features;
using CMS.Modules.News.Application.Interfaces;
using CMS.Modules.News.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Modules.News.Web.Controllers;

/// <summary>Permanent redirects from the old /news URLs to /education-articles.</summary>
[Route("news")]
public class NewsLegacyRedirectController : Controller
{
    private readonly IPublicArticleQuery _articles;
    private readonly IFeatureManager _features;

    public NewsLegacyRedirectController(IPublicArticleQuery articles, IFeatureManager features)
    {
        _articles = articles;
        _features = features;
    }

    [HttpGet("")]
    public IActionResult Index() => RedirectPermanent("/education-articles/");

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        var normalized = slug.Trim().Trim('/').ToLowerInvariant();
        var post = await _articles.GetPublishedBySlugAsync(normalized, cancellationToken);
        if (post is null)
            return NotFound();

        if (post.Kind == ArticleKind.Event)
            return RedirectPermanent($"/event/{post.Slug}/");

        return RedirectPermanent($"/education-articles/{post.Slug}/");
    }
}

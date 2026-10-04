using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Application.Seo;
using CMS.Modules.News.Application.Interfaces;
using CMS.Modules.News.Domain.Enums;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Modules.News.Web.Controllers;

[Route("event")]
public class EventsController : Controller
{
    private readonly IPublicArticleQuery _articles;
    private readonly IFeatureManager _features;
    private readonly ISeoDocumentService _seoDocuments;

    public EventsController(
        IPublicArticleQuery articles,
        IFeatureManager features,
        ISeoDocumentService seoDocuments)
    {
        _articles = articles;
        _features = features;
        _seoDocuments = seoDocuments;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["page"])]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        ViewData["Title"] = "رویدادها";
        ViewData["NavActive"] = "events";
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Manage("NewsArticles", "مدیریت رویدادها", "ViewNews");

        var result = await _articles.ListPublishedByKindPagedAsync(ArticleKind.Event, page, 48, cancellationToken);
        ViewBag.Page = result.Page;
        ViewBag.PageSize = result.PageSize;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.TotalPages = result.TotalPages;
        return View(result.Items);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        var post = await _articles.GetPublishedBySlugAsync(slug, cancellationToken);
        if (post is null || post.Kind != ArticleKind.Event)
            return NotFound();

        ViewData["Title"] = post.Title;
        ViewData["NavActive"] = "events";
        ViewData["MetaTitle"] = FirstNonEmpty(post.MetaTitle, post.Title);
        ViewData["MetaDescription"] = FirstNonEmpty(post.MetaDescription, post.Excerpt);
        ViewData["MetaKeywords"] = post.SeoKeywords;
        ViewData["CanonicalUrl"] = post.CanonicalUrl;
        ViewData["OgTitle"] = FirstNonEmpty(post.OgTitle, post.MetaTitle, post.Title);
        ViewData["OgDescription"] = FirstNonEmpty(post.OgDescription, post.MetaDescription, post.Excerpt);
        ViewData["OgImage"] = FirstNonEmpty(post.OgImageUrl, post.CoverImageUrl);
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("NewsArticles", post.Id, "ویرایش رویداد", "ManageNews");

        var related = await _articles.ListPublishedByKindPagedAsync(ArticleKind.Event, 1, 6, cancellationToken);
        ViewBag.Related = related.Items
            .Where(x => !string.Equals(x.Slug, post.Slug, StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        if (await _features.IsEnabledAsync(FeatureNames.Seo))
        {
            var seo = await _seoDocuments.GetAsync(SeoContentTypeKeys.NewsArticle, post.Id, cancellationToken);
            if (seo is not null)
            {
                ViewData["Robots"] = SeoEditorHelper.FormatRobotsMeta(seo.RobotsIndex, seo.RobotsFollow);
                ViewData["SchemaJson"] = FirstNonEmpty(
                    seo.SchemaJson,
                    SeoSchemaBuilder.BuildContentSchema(
                        seo.SchemaType ?? "Event",
                        post.Title,
                        FirstNonEmpty(post.MetaDescription, post.Excerpt),
                        AbsoluteUrl(post.CanonicalUrl, $"/event/{post.Slug}"),
                        FirstNonEmpty(post.OgImageUrl, post.CoverImageUrl),
                        post.AuthorDisplayName,
                        post.EventStartAtUtc ?? post.PublishedAtUtc,
                        null));
            }
        }

        return View(post);
    }

    private string? AbsoluteUrl(string? canonical, string relativePath)
    {
        if (!string.IsNullOrWhiteSpace(canonical))
            return canonical.Trim();
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

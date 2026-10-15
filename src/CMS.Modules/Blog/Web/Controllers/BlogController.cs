using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Application.Seo;
using CMS.Modules.Blog.Application.Interfaces;
using CMS.Modules.Blog.Application.Posts;
using CMS.Modules.Blog.Web;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Blog.Web.Controllers;

[Route("blog")]
public class BlogController : Controller
{
    private readonly IPublicPostQuery _posts;
    private readonly IFeatureManager _features;
    private readonly ISeoDocumentService _seoDocuments;
    private readonly IStringLocalizer<BlogPublic> _localizer;

    public BlogController(
        IPublicPostQuery posts,
        IFeatureManager features,
        ISeoDocumentService seoDocuments,
        IStringLocalizer<BlogPublic> localizer)
    {
        _posts = posts;
        _features = features;
        _seoDocuments = seoDocuments;
        _localizer = localizer;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 90, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["page"])]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Blog))
            return NotFound();

        ViewData["Title"] = "وبلاگ";
        ViewData["NavActive"] = "blog";
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Manage("Posts", "مدیریت نوشته‌ها", "ViewBlog");
        var result = await _posts.ListPublishedPagedAsync(
            page,
            IPublicPostQuery.DefaultArchivePageSize,
            cancellationToken);
        if (result.TotalPages > 0 && page > result.TotalPages)
            return RedirectToAction(nameof(Index), new { page = result.TotalPages });

        ViewBag.Page = result.Page;
        ViewBag.PageSize = result.PageSize;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.TotalPages = result.TotalPages;
        return View(result.Items);
    }

    /// <summary>Old /blog/{slug} URLs permanently redirect to root /{slug} when the post exists.</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> LegacyDetails(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        if (!await _features.IsEnabledAsync(FeatureNames.Blog))
            return NotFound();

        var normalized = slug.Trim().Trim('/').ToLowerInvariant();
        var post = await _posts.GetPublishedBySlugAsync(normalized, cancellationToken);
        if (post is null)
            return NotFound();

        return RedirectPermanent($"/{post.Slug}/");
    }

    // Low priority so /admin, /shop, /services, … keep their own routes.
    [HttpGet("/{slug:regex(^(?!admin$|api$|account$|blog$|shop$|services$|doctors$|teams$|news$|event$|events$|videos$|forms$|contact$|about$|health$|sitemaps$|sitemap$|sitemap\\.xml$|robots\\.txt$).+)}", Name = "blog-post", Order = 1000)]
    [ResponseCache(Duration = 90, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["commentPage"])]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Blog))
            return NotFound();

        var post = await _posts.GetPublishedBySlugAsync(slug, cancellationToken);
        if (post is null)
            return NotFound();

        var publicPath = $"/{post.Slug}";
        ViewData["Title"] = post.Title;
        ViewData["NavActive"] = "blog";
        ViewData["MetaTitle"] = FirstNonEmpty(post.MetaTitle, post.Title);
        ViewData["MetaDescription"] = FirstNonEmpty(post.MetaDescription, post.Excerpt);
        ViewData["MetaKeywords"] = post.SeoKeywords;
        ViewData["CanonicalUrl"] = AbsoluteUrl(post.CanonicalUrl, publicPath);
        ViewData["OgTitle"] = FirstNonEmpty(post.OgTitle, post.MetaTitle, post.Title);
        ViewData["OgDescription"] = FirstNonEmpty(post.OgDescription, post.MetaDescription, post.Excerpt);
        ViewData["OgImage"] = FirstNonEmpty(post.OgImageUrl, post.CoverImageUrl);
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("Posts", post.Id, "ویرایش نوشته", "ManageBlog");

        var relatedIds = PostRelatedPostsJson.Parse(post.RelatedPostsJson);
        IReadOnlyList<PublicPostSummaryDto> relatedItems = [];
        if (relatedIds.Count > 0)
        {
            relatedItems = await _posts.ListPublishedByIdsPreserveOrderAsync(relatedIds, cancellationToken);
            relatedItems = relatedItems
                .Where(x => !string.Equals(x.Slug, post.Slug, StringComparison.OrdinalIgnoreCase))
                .Take(PostRelatedPostsJson.MaxItems)
                .ToList();
        }

        if (relatedItems.Count == 0)
        {
            var related = await _posts.ListPublishedPagedAsync(1, 8, cancellationToken);
            relatedItems = related.Items
                .Where(x => !string.Equals(x.Slug, post.Slug, StringComparison.OrdinalIgnoreCase))
                .Take(3)
                .ToList();
        }

        ViewBag.Related = relatedItems;

        if (await _features.IsEnabledAsync(FeatureNames.Seo))
        {
            var seo = await _seoDocuments.GetAsync(SeoContentTypeKeys.BlogPost, post.Id, cancellationToken);
            if (seo is not null)
            {
                ViewData["Robots"] = SeoEditorHelper.FormatRobotsMeta(seo.RobotsIndex, seo.RobotsFollow);
                ViewData["SchemaJson"] = FirstNonEmpty(
                    seo.SchemaJson,
                    SeoSchemaBuilder.BuildContentSchema(
                        seo.SchemaType,
                        post.Title,
                        FirstNonEmpty(post.MetaDescription, post.Excerpt),
                        AbsoluteUrl(post.CanonicalUrl, publicPath),
                        FirstNonEmpty(post.OgImageUrl, post.CoverImageUrl),
                        post.AuthorDisplayName,
                        post.PublishedAtUtc,
                        post.UpdatedAtUtc));
            }
        }

        return View(post);
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

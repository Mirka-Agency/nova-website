using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Application.Seo;
using CMS.Modules.Blog.Application.Interfaces;
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
        // Archive matches Nova blog template (client-side paging in main.js).
        var result = await _posts.ListPublishedPagedAsync(page, 48, cancellationToken);
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
        if (!await _features.IsEnabledAsync(FeatureNames.Blog))
            return NotFound();

        var post = await _posts.GetPublishedBySlugAsync(slug, cancellationToken);
        if (post is null)
            return NotFound();

        ViewData["Title"] = post.Title;
        ViewData["NavActive"] = "blog";
        ViewData["MetaTitle"] = FirstNonEmpty(post.MetaTitle, post.Title);
        ViewData["MetaDescription"] = FirstNonEmpty(post.MetaDescription, post.Excerpt);
        ViewData["MetaKeywords"] = post.SeoKeywords;
        ViewData["CanonicalUrl"] = post.CanonicalUrl;
        ViewData["OgTitle"] = FirstNonEmpty(post.OgTitle, post.MetaTitle, post.Title);
        ViewData["OgDescription"] = FirstNonEmpty(post.OgDescription, post.MetaDescription, post.Excerpt);
        ViewData["OgImage"] = FirstNonEmpty(post.OgImageUrl, post.CoverImageUrl);
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("Posts", post.Id, "ویرایش نوشته", "ManageBlog");

        var related = await _posts.ListPublishedPagedAsync(1, 8, cancellationToken);
        ViewBag.Related = related.Items
            .Where(x => !string.Equals(x.Slug, post.Slug, StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

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
                        AbsoluteUrl(post.CanonicalUrl, $"/blog/{post.Slug}"),
                        FirstNonEmpty(post.OgImageUrl, post.CoverImageUrl),
                        post.AuthorDisplayName,
                        post.PublishedAtUtc,
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

using CMS.Application.Caching;
using CMS.Application.Common.Paging;
using CMS.Modules.News.Application.Articles;
using CMS.Modules.News.Application.Interfaces;
using CMS.Modules.News.Domain.Enums;
using CMS.Modules.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CMS.Modules.News.Infrastructure.Services;

public sealed class PublicArticleQuery : IPublicArticleQuery
{
    public const int DefaultPageSize = 12;
    private const int ExcerptLength = 180;
    private readonly NewsDbContext _db;
    private readonly IMemoryCache _cache;

    public PublicArticleQuery(NewsDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<PublicArticleSummaryDto>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        var page = await ListPublishedPagedAsync(1, DefaultPageSize, cancellationToken);
        return page.Items;
    }

    public Task<PagedResult<PublicArticleSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        ListPublishedByKindPagedAsync(kind: null, page, pageSize, cancellationToken);

    public Task<PagedResult<PublicArticleSummaryDto>> ListPublishedByKindPagedAsync(
        ArticleKind kind,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        ListPublishedByKindPagedAsync((ArticleKind?)kind, page, pageSize, cancellationToken);

    private Task<PagedResult<PublicArticleSummaryDto>> ListPublishedByKindPagedAsync(
        ArticleKind? kind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, 48);
        var kindKey = kind?.ToString() ?? "all";
        var key = $"cms:public:news:list:{kindKey}:{normalizedPage}:{normalizedPageSize}";

        return PublicContentCache.GetOrCreateAsync(
            _cache,
            key,
            ct => ListPublishedByKindPagedCoreAsync(kind, normalizedPage, normalizedPageSize, ct),
            cancellationToken);
    }

    private async Task<PagedResult<PublicArticleSummaryDto>> ListPublishedByKindPagedCoreAsync(
        ArticleKind? kind,
        int normalizedPage,
        int normalizedPageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Articles
            .AsNoTracking()
            .Where(p => p.Status == ArticleStatus.Published);

        if (kind.HasValue)
            query = query.Where(p => p.Kind == kind.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = kind == ArticleKind.Event
            ? query.OrderByDescending(p => p.EventStartAtUtc ?? p.PublishedAtUtc ?? p.CreatedAtUtc)
            : query.OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc);

        var rows = await ordered
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(p => new
            {
                p.Title,
                p.Slug,
                p.Kind,
                CategoryName = p.Category != null ? p.Category.Name : null,
                CategorySlug = p.Category != null ? p.Category.Slug : null,
                p.CoverImageUrl,
                p.CoverImageAlt,
                PublishedAtUtc = p.PublishedAtUtc ?? p.CreatedAtUtc,
                p.EventStartAtUtc,
                p.EventEndAtUtc,
                p.Location,
                p.Excerpt,
                p.Body,
                p.AuthorDisplayName
            })
            .ToListAsync(cancellationToken);

        var articles = rows
            .Select(p => new PublicArticleSummaryDto(
                p.Title,
                p.Slug,
                p.Kind,
                p.CategoryName,
                p.CategorySlug,
                p.CoverImageUrl,
                p.CoverImageAlt,
                p.PublishedAtUtc,
                p.EventStartAtUtc,
                p.EventEndAtUtc,
                p.Location,
                ResolveExcerpt(p.Excerpt, p.Body),
                p.AuthorDisplayName))
            .ToList();

        return new PagedResult<PublicArticleSummaryDto>(articles, totalCount, normalizedPage, normalizedPageSize);
    }

    public Task<IReadOnlyList<PublicArticleCategoryDto>> ListPublishedCategoriesAsync(
        ArticleKind kind,
        CancellationToken cancellationToken = default)
    {
        var key = $"cms:public:news:categories:{kind}";
        return PublicContentCache.GetOrCreateAsync(
            _cache,
            key,
            async ct => (IReadOnlyList<PublicArticleCategoryDto>)await _db.Categories
                .AsNoTracking()
                .Where(c => c.Articles.Any(a => a.Status == ArticleStatus.Published && a.Kind == kind))
                .OrderBy(c => c.Name)
                .Select(c => new PublicArticleCategoryDto(c.Name, c.Slug))
                .ToListAsync(ct),
            cancellationToken);
    }

    public Task<PublicArticleDetailDto?> GetPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Task.FromResult<PublicArticleDetailDto?>(null);

        var normalized = slug.Trim().ToLowerInvariant();
        var key = $"cms:public:news:slug:{normalized}";

        return PublicContentCache.GetOrCreateNullableAsync(
            _cache,
            key,
            ct => GetPublishedBySlugCoreAsync(normalized, ct),
            cancellationToken);
    }

    private async Task<PublicArticleDetailDto?> GetPublishedBySlugCoreAsync(
        string normalized,
        CancellationToken cancellationToken)
    {
        var article = await _db.Articles
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(
                p => p.Slug == normalized && p.Status == ArticleStatus.Published,
                cancellationToken);

        if (article is null)
            return null;

        return new PublicArticleDetailDto(
            article.Id,
            article.Title,
            article.Slug,
            article.Body,
            article.Kind,
            article.Category?.Name,
            article.Category?.Slug,
            article.CoverImageUrl,
            article.CoverImageAlt,
            article.GalleryJson,
            article.AttachmentUrl,
            article.AttachmentFileName,
            article.PublishedAtUtc ?? article.CreatedAtUtc,
            article.EventStartAtUtc,
            article.EventEndAtUtc,
            article.Location,
            article.EventInfoJson,
            ResolveExcerpt(article.Excerpt, article.Body),
            article.AuthorDisplayName,
            article.MetaTitle,
            article.MetaDescription,
            article.SeoKeywords,
            article.CanonicalUrl,
            article.OgTitle,
            article.OgDescription,
            article.OgImageUrl);
    }

    private static string ResolveExcerpt(string? excerpt, string body) =>
        MakeExcerpt(!string.IsNullOrWhiteSpace(excerpt) ? excerpt : body);

    private static string MakeExcerpt(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        var plain = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]+>", " ");
        plain = System.Net.WebUtility.HtmlDecode(plain);
        var normalized = string.Join(' ', plain.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length <= ExcerptLength)
            return normalized;

        return normalized[..ExcerptLength].TrimEnd() + "…";
    }
}

using CMS.Application.Common.Paging;
using CMS.Modules.News.Application.Articles;
using CMS.Modules.News.Application.Interfaces;
using CMS.Modules.News.Domain.Enums;
using CMS.Modules.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.News.Infrastructure.Services;

public sealed class PublicArticleQuery : IPublicArticleQuery
{
    public const int DefaultPageSize = 12;
    private const int ExcerptLength = 180;
    private readonly NewsDbContext _db;

    public PublicArticleQuery(NewsDbContext db)
    {
        _db = db;
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

    public async Task<PagedResult<PublicArticleSummaryDto>> ListPublishedByKindPagedAsync(
        ArticleKind kind,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        await ListPublishedByKindPagedAsync((ArticleKind?)kind, page, pageSize, cancellationToken);

    private async Task<PagedResult<PublicArticleSummaryDto>> ListPublishedByKindPagedAsync(
        ArticleKind? kind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, 48);

        var query = _db.Articles
            .AsNoTracking()
            .Where(p => p.Status == ArticleStatus.Published);

        if (kind.HasValue)
            query = query.Where(p => p.Kind == kind.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = kind == ArticleKind.Event
            ? query.OrderByDescending(p => p.EventStartAtUtc ?? p.PublishedAtUtc ?? p.CreatedAtUtc)
            : query.OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc);

        var articles = await ordered
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(p => new PublicArticleSummaryDto(
                p.Title,
                p.Slug,
                p.Kind,
                p.Category != null ? p.Category.Name : null,
                p.Category != null ? p.Category.Slug : null,
                p.CoverImageUrl,
                p.PublishedAtUtc ?? p.CreatedAtUtc,
                p.EventStartAtUtc,
                p.EventEndAtUtc,
                p.Location,
                p.Excerpt != null && p.Excerpt != string.Empty
                    ? p.Excerpt
                    : (p.Body.Length > ExcerptLength ? p.Body.Substring(0, ExcerptLength) : p.Body),
                p.AuthorDisplayName))
            .ToListAsync(cancellationToken);

        return new PagedResult<PublicArticleSummaryDto>(articles, totalCount, normalizedPage, normalizedPageSize);
    }

    public async Task<IReadOnlyList<PublicArticleCategoryDto>> ListPublishedCategoriesAsync(
        ArticleKind kind,
        CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.Articles.Any(a => a.Status == ArticleStatus.Published && a.Kind == kind))
            .OrderBy(c => c.Name)
            .Select(c => new PublicArticleCategoryDto(c.Name, c.Slug))
            .ToListAsync(cancellationToken);
    }

    public async Task<PublicArticleDetailDto?> GetPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var normalized = slug.Trim().ToLowerInvariant();
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
            article.GalleryJson,
            article.AttachmentUrl,
            article.AttachmentFileName,
            article.PublishedAtUtc ?? article.CreatedAtUtc,
            article.EventStartAtUtc,
            article.EventEndAtUtc,
            article.Location,
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
        !string.IsNullOrWhiteSpace(excerpt) ? excerpt.Trim() : MakeExcerpt(body);

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

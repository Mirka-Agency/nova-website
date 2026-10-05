using CMS.Application.Caching;
using CMS.Application.Common.Paging;
using CMS.Modules.Video.Application.Interfaces;
using CMS.Modules.Video.Application.VideoItems;
using CMS.Modules.Video.Domain.Enums;
using CMS.Modules.Video.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CMS.Modules.Video.Infrastructure.Services;

public sealed class PublicVideoItemQuery : IPublicVideoItemQuery
{
    public const int DefaultPageSize = 12;
    private const int ExcerptLength = 180;
    private readonly VideoDbContext _db;
    private readonly IMemoryCache _cache;

    public PublicVideoItemQuery(VideoDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<PublicVideoItemSummaryDto>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        var page = await ListPublishedPagedAsync(1, DefaultPageSize, cancellationToken);
        return page.Items;
    }

    public Task<PagedResult<PublicVideoItemSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, 48);
        var key = $"cms:public:video:list:{normalizedPage}:{normalizedPageSize}";

        return PublicContentCache.GetOrCreateAsync(
            _cache,
            key,
            ct => ListPublishedPagedCoreAsync(normalizedPage, normalizedPageSize, ct),
            cancellationToken);
    }

    private async Task<PagedResult<PublicVideoItemSummaryDto>> ListPublishedPagedCoreAsync(
        int normalizedPage,
        int normalizedPageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.VideoItems
            .AsNoTracking()
            .Where(p => p.Status == VideoStatus.Published);

        var totalCount = await query.CountAsync(cancellationToken);

        var posts = await query
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(p => new PublicVideoItemSummaryDto(
                p.Title,
                p.Slug,
                p.Category != null ? p.Category.Name : null,
                p.Category != null ? p.Category.Slug : null,
                p.CoverImageUrl,
                p.VideoUrl,
                p.PublishedAtUtc ?? p.CreatedAtUtc,
                p.Excerpt != null && p.Excerpt != string.Empty
                    ? p.Excerpt
                    : (p.Body.Length > ExcerptLength ? p.Body.Substring(0, ExcerptLength) : p.Body),
                p.AuthorDisplayName))
            .ToListAsync(cancellationToken);

        return new PagedResult<PublicVideoItemSummaryDto>(posts, totalCount, normalizedPage, normalizedPageSize);
    }

    public Task<IReadOnlyList<PublicVideoCategoryDto>> ListPublishedCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        const string key = "cms:public:video:categories";
        return PublicContentCache.GetOrCreateAsync(
            _cache,
            key,
            async ct => (IReadOnlyList<PublicVideoCategoryDto>)await _db.Categories
                .AsNoTracking()
                .Where(c => c.VideoItems.Any(v => v.Status == VideoStatus.Published))
                .OrderBy(c => c.Name)
                .Select(c => new PublicVideoCategoryDto(c.Name, c.Slug))
                .ToListAsync(ct),
            cancellationToken);
    }

    public Task<PublicVideoItemDetailDto?> GetPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Task.FromResult<PublicVideoItemDetailDto?>(null);

        var normalized = slug.Trim().ToLowerInvariant();
        var key = $"cms:public:video:slug:{normalized}";

        return PublicContentCache.GetOrCreateNullableAsync(
            _cache,
            key,
            ct => GetPublishedBySlugCoreAsync(normalized, ct),
            cancellationToken);
    }

    private async Task<PublicVideoItemDetailDto?> GetPublishedBySlugCoreAsync(
        string normalized,
        CancellationToken cancellationToken)
    {
        var post = await _db.VideoItems
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(
                p => p.Slug == normalized && p.Status == VideoStatus.Published,
                cancellationToken);

        if (post is null)
            return null;

        return new PublicVideoItemDetailDto(
            post.Id,
            post.Title,
            post.Slug,
            post.Body,
            post.Category?.Name,
            post.Category?.Slug,
            post.CoverImageUrl,
            post.VideoUrl,
            post.PublishedAtUtc ?? post.CreatedAtUtc,
            ResolveExcerpt(post.Excerpt, post.Body),
            post.AuthorDisplayName,
            post.MetaTitle,
            post.MetaDescription,
            post.SeoKeywords,
            post.CanonicalUrl,
            post.OgTitle,
            post.OgDescription,
            post.OgImageUrl);
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

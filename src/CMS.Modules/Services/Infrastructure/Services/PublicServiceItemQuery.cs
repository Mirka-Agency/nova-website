using CMS.Application.Caching;
using CMS.Application.Common.Paging;
using CMS.Modules.Services.Application.Interfaces;
using CMS.Modules.Services.Application.ServiceItems;
using CMS.Modules.Services.Domain.Enums;
using CMS.Modules.Services.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CMS.Modules.Services.Infrastructure.Services;

public sealed class PublicServiceItemQuery : IPublicServiceItemQuery
{
    public const int DefaultPageSize = 12;
    private const int ExcerptLength = 180;
    private readonly ServicesDbContext _db;
    private readonly IMemoryCache _cache;

    public PublicServiceItemQuery(ServicesDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<PublicServiceItemSummaryDto>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        var page = await ListPublishedPagedAsync(1, DefaultPageSize, cancellationToken);
        return page.Items;
    }

    public Task<PagedResult<PublicServiceItemSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, 48);
        var key = $"cms:public:services:list:{normalizedPage}:{normalizedPageSize}";

        return PublicContentCache.GetOrCreateAsync(
            _cache,
            key,
            ct => ListPublishedPagedCoreAsync(normalizedPage, normalizedPageSize, ct),
            cancellationToken);
    }

    private async Task<PagedResult<PublicServiceItemSummaryDto>> ListPublishedPagedCoreAsync(
        int normalizedPage,
        int normalizedPageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.ServiceItems
            .AsNoTracking()
            .Where(p => p.Status == ServiceStatus.Published);

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(p => new
            {
                p.Title,
                p.Slug,
                CategoryName = p.Category != null ? p.Category.Name : null,
                p.CoverImageUrl,
                PublishedAtUtc = p.PublishedAtUtc ?? p.CreatedAtUtc,
                p.Excerpt,
                p.Body,
                p.AuthorDisplayName
            })
            .ToListAsync(cancellationToken);

        var posts = rows
            .Select(p => new PublicServiceItemSummaryDto(
                p.Title,
                p.Slug,
                p.CategoryName,
                p.CoverImageUrl,
                p.PublishedAtUtc,
                ResolveExcerpt(p.Excerpt, p.Body),
                p.AuthorDisplayName))
            .ToList();

        return new PagedResult<PublicServiceItemSummaryDto>(posts, totalCount, normalizedPage, normalizedPageSize);
    }

    public Task<PublicServiceItemDetailDto?> GetPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Task.FromResult<PublicServiceItemDetailDto?>(null);

        var normalized = slug.Trim().ToLowerInvariant();
        var key = $"cms:public:services:slug:{normalized}";

        return PublicContentCache.GetOrCreateNullableAsync(
            _cache,
            key,
            ct => GetPublishedBySlugCoreAsync(normalized, ct),
            cancellationToken);
    }

    private async Task<PublicServiceItemDetailDto?> GetPublishedBySlugCoreAsync(
        string normalized,
        CancellationToken cancellationToken)
    {
        var post = await _db.ServiceItems
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(
                p => p.Slug == normalized && p.Status == ServiceStatus.Published,
                cancellationToken);

        if (post is null)
            return null;

        return new PublicServiceItemDetailDto(
            post.Id,
            post.Title,
            post.Slug,
            post.Body,
            post.Category?.Name,
            post.CoverImageUrl,
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

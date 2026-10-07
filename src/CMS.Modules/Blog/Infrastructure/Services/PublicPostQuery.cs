using CMS.Application.Caching;
using CMS.Application.Common.Paging;
using CMS.Modules.Blog.Application.Interfaces;
using CMS.Modules.Blog.Application.Posts;
using CMS.Modules.Blog.Domain.Enums;
using CMS.Modules.Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CMS.Modules.Blog.Infrastructure.Services;

public sealed class PublicPostQuery : IPublicPostQuery
{
    public const int DefaultPageSize = 12;
    private const int ExcerptLength = 180;
    private readonly BlogDbContext _db;
    private readonly IMemoryCache _cache;

    public PublicPostQuery(BlogDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<PublicPostSummaryDto>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        var page = await ListPublishedPagedAsync(1, DefaultPageSize, cancellationToken);
        return page.Items;
    }

    public Task<PagedResult<PublicPostSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, 48);
        var key = $"cms:public:blog:list:{normalizedPage}:{normalizedPageSize}";

        return PublicContentCache.GetOrCreateAsync(
            _cache,
            key,
            ct => ListPublishedPagedCoreAsync(normalizedPage, normalizedPageSize, ct),
            cancellationToken);
    }

    private async Task<PagedResult<PublicPostSummaryDto>> ListPublishedPagedCoreAsync(
        int normalizedPage,
        int normalizedPageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Posts
            .AsNoTracking()
            .Where(p => p.Status == PostStatus.Published);

        var totalCount = await query.CountAsync(cancellationToken);

        var posts = await query
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(p => new PublicPostSummaryDto(
                p.Title,
                p.Slug,
                p.Category != null ? p.Category.Name : null,
                p.CoverImageUrl,
                p.CoverImageAlt,
                p.PublishedAtUtc ?? p.CreatedAtUtc,
                p.Excerpt != null && p.Excerpt != string.Empty
                    ? p.Excerpt
                    : (p.Body.Length > ExcerptLength ? p.Body.Substring(0, ExcerptLength) : p.Body),
                p.AuthorDisplayName))
            .ToListAsync(cancellationToken);

        return new PagedResult<PublicPostSummaryDto>(posts, totalCount, normalizedPage, normalizedPageSize);
    }

    public Task<PublicPostDetailDto?> GetPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Task.FromResult<PublicPostDetailDto?>(null);

        var normalized = slug.Trim().ToLowerInvariant();
        var key = $"cms:public:blog:slug:{normalized}";

        return PublicContentCache.GetOrCreateNullableAsync(
            _cache,
            key,
            ct => GetPublishedBySlugCoreAsync(normalized, ct),
            cancellationToken);
    }

    private async Task<PublicPostDetailDto?> GetPublishedBySlugCoreAsync(
        string normalized,
        CancellationToken cancellationToken)
    {
        var post = await _db.Posts
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(
                p => p.Slug == normalized && p.Status == PostStatus.Published,
                cancellationToken);

        if (post is null)
            return null;

        return new PublicPostDetailDto(
            post.Id,
            post.Title,
            post.Slug,
            post.Body,
            post.Category?.Name,
            post.CoverImageUrl,
            post.CoverImageAlt,
            post.CoverVideoUrl,
            post.PublishedAtUtc ?? post.CreatedAtUtc,
            ResolveExcerpt(post.Excerpt, post.Body),
            post.AuthorDisplayName,
            post.MetaTitle,
            post.MetaDescription,
            post.SeoKeywords,
            post.CanonicalUrl,
            post.OgTitle,
            post.OgDescription,
            post.OgImageUrl,
            post.FaqJson);
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

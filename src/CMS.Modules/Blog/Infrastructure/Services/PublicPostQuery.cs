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
    public const int DefaultPageSize = IPublicPostQuery.DefaultArchivePageSize;
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
        var page = await ListPublishedPagedAsync(1, IPublicPostQuery.FirstArchivePageSize, cancellationToken);
        return page.Items;
    }

    public Task<PagedResult<PublicPostSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        // Archive uses asymmetric first-page size; ignore arbitrary pageSize from callers.
        var key = $"cms:public:blog:list:{normalizedPage}:archive";

        return PublicContentCache.GetOrCreateAsync(
            _cache,
            key,
            ct => ListPublishedPagedCoreAsync(normalizedPage, ct),
            cancellationToken);
    }

    private async Task<PagedResult<PublicPostSummaryDto>> ListPublishedPagedCoreAsync(
        int normalizedPage,
        CancellationToken cancellationToken)
    {
        var query = _db.Posts
            .AsNoTracking()
            .Where(p => p.Status == PostStatus.Published);

        var totalCount = await query.CountAsync(cancellationToken);
        var (skip, take, totalPages, resultPageSize) = ResolveArchiveSlice(normalizedPage, totalCount);

        var rows = await query
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(p => new
            {
                p.Title,
                p.Slug,
                CategoryName = p.Category != null ? p.Category.Name : null,
                p.CoverImageUrl,
                p.CoverImageAlt,
                PublishedAtUtc = p.PublishedAtUtc ?? p.CreatedAtUtc,
                UpdatedAtUtc = p.UpdatedAtUtc ?? p.PublishedAtUtc ?? p.CreatedAtUtc,
                p.Excerpt,
                p.Body,
                p.AuthorDisplayName
            })
            .ToListAsync(cancellationToken);

        var posts = rows
            .Select(p => new PublicPostSummaryDto(
                p.Title,
                p.Slug,
                p.CategoryName,
                p.CoverImageUrl,
                p.CoverImageAlt,
                p.PublishedAtUtc,
                p.UpdatedAtUtc,
                ResolveExcerpt(p.Excerpt, p.Body),
                p.AuthorDisplayName))
            .ToList();

        // PageSize is this slice's take; TotalPagesOverride accounts for larger first page.
        return new PagedResult<PublicPostSummaryDto>(
            posts,
            totalCount,
            normalizedPage,
            resultPageSize,
            totalPages);
    }

    /// <summary>
    /// Page 1: 13 posts (featured + 12). Later pages: 12 each.
    /// </summary>
    internal static (int Skip, int Take, int TotalPages, int PageSize) ResolveArchiveSlice(
        int page,
        int totalCount)
    {
        const int first = IPublicPostQuery.FirstArchivePageSize;
        const int rest = IPublicPostQuery.DefaultArchivePageSize;

        if (totalCount <= 0)
            return (0, first, 0, first);

        var totalPages = totalCount <= first
            ? 1
            : 1 + (int)Math.Ceiling((totalCount - first) / (double)rest);

        if (page <= 1)
            return (0, first, totalPages, first);

        if (page > totalPages)
            page = totalPages;

        var skip = first + (page - 2) * rest;
        return (skip, rest, totalPages, rest);
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
            post.CategoryId,
            post.CoverImageUrl,
            post.CoverImageAlt,
            post.CoverVideoUrl,
            post.PublishedAtUtc ?? post.CreatedAtUtc,
            post.UpdatedAtUtc ?? post.PublishedAtUtc ?? post.CreatedAtUtc,
            ResolveExcerpt(post.Excerpt, post.Body),
            post.AuthorDisplayName,
            post.MetaTitle,
            post.MetaDescription,
            post.SeoKeywords,
            post.CanonicalUrl,
            post.OgTitle,
            post.OgDescription,
            post.OgImageUrl,
            post.FaqJson,
            post.VideoSchemaJson,
            post.ContentGuideJson,
            post.SourcesJson,
            post.RelatedPostsJson);
    }

    public async Task<IReadOnlyList<PublicPostSummaryDto>> ListPublishedByIdsPreserveOrderAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids is null || ids.Count == 0)
            return [];

        var unique = ids.Where(x => x != Guid.Empty).Distinct().Take(IPublicPostQuery.MaxArchivePageSize).ToList();
        if (unique.Count == 0)
            return [];

        var rows = await _db.Posts
            .AsNoTracking()
            .Where(p => p.Status == PostStatus.Published && unique.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                CategoryName = p.Category != null ? p.Category.Name : null,
                p.CoverImageUrl,
                p.CoverImageAlt,
                PublishedAtUtc = p.PublishedAtUtc ?? p.CreatedAtUtc,
                UpdatedAtUtc = p.UpdatedAtUtc ?? p.PublishedAtUtc ?? p.CreatedAtUtc,
                p.Excerpt,
                p.Body,
                p.AuthorDisplayName
            })
            .ToListAsync(cancellationToken);

        var byId = rows.ToDictionary(x => x.Id);
        var result = new List<PublicPostSummaryDto>();
        foreach (var id in ids)
        {
            if (!byId.TryGetValue(id, out var p))
                continue;
            result.Add(new PublicPostSummaryDto(
                p.Title,
                p.Slug,
                p.CategoryName,
                p.CoverImageUrl,
                p.CoverImageAlt,
                p.PublishedAtUtc,
                p.UpdatedAtUtc,
                ResolveExcerpt(p.Excerpt, p.Body),
                p.AuthorDisplayName));
        }

        return result;
    }

    public async Task<IReadOnlyList<PublicPostSummaryDto>> ListPublishedRelatedByCategoryAsync(
        Guid categoryId,
        Guid excludeId,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (categoryId == Guid.Empty || take < 1)
            return [];

        var limit = Math.Min(take, IPublicPostQuery.MaxArchivePageSize);
        var rows = await _db.Posts
            .AsNoTracking()
            .Where(p =>
                p.Status == PostStatus.Published
                && p.CategoryId == categoryId
                && p.Id != excludeId)
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .Take(limit)
            .Select(p => new
            {
                p.Title,
                p.Slug,
                CategoryName = p.Category != null ? p.Category.Name : null,
                p.CoverImageUrl,
                p.CoverImageAlt,
                PublishedAtUtc = p.PublishedAtUtc ?? p.CreatedAtUtc,
                UpdatedAtUtc = p.UpdatedAtUtc ?? p.PublishedAtUtc ?? p.CreatedAtUtc,
                p.Excerpt,
                p.Body,
                p.AuthorDisplayName
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(p => new PublicPostSummaryDto(
                p.Title,
                p.Slug,
                p.CategoryName,
                p.CoverImageUrl,
                p.CoverImageAlt,
                p.PublishedAtUtc,
                p.UpdatedAtUtc,
                ResolveExcerpt(p.Excerpt, p.Body),
                p.AuthorDisplayName))
            .ToList();
    }

    public async Task<IReadOnlyList<Guid>> ListPublishedIdsReferencingAsync(
        Guid postId,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (postId == Guid.Empty || take < 1)
            return [];

        var needle = postId.ToString("D");
        var limit = Math.Min(take, IPublicPostQuery.MaxArchivePageSize);

        // RelatedPostsJson stores GUID strings; Contains is enough for exact D-format ids.
        return await _db.Posts
            .AsNoTracking()
            .Where(p =>
                p.Status == PostStatus.Published
                && p.Id != postId
                && p.RelatedPostsJson != null
                && p.RelatedPostsJson.Contains(needle))
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .Take(limit)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
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

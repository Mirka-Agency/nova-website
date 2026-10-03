using CMS.Application.Common.Paging;
using CMS.Modules.Services.Application.Interfaces;
using CMS.Modules.Services.Application.ServiceItems;
using CMS.Modules.Services.Domain.Enums;
using CMS.Modules.Services.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Services.Infrastructure.Services;

public sealed class PublicServiceItemQuery : IPublicServiceItemQuery
{
    public const int DefaultPageSize = 12;
    private const int ExcerptLength = 180;
    private readonly ServicesDbContext _db;

    public PublicServiceItemQuery(ServicesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PublicServiceItemSummaryDto>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        var page = await ListPublishedPagedAsync(1, DefaultPageSize, cancellationToken);
        return page.Items;
    }

    public async Task<PagedResult<PublicServiceItemSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, 48);

        var query = _db.ServiceItems
            .AsNoTracking()
            .Where(p => p.Status == ServiceStatus.Published);

        var totalCount = await query.CountAsync(cancellationToken);

        var posts = await query
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(p => new PublicServiceItemSummaryDto(
                p.Title,
                p.Slug,
                p.Category != null ? p.Category.Name : null,
                p.CoverImageUrl,
                p.PublishedAtUtc ?? p.CreatedAtUtc,
                p.Excerpt != null && p.Excerpt != string.Empty
                    ? p.Excerpt
                    : (p.Body.Length > ExcerptLength ? p.Body.Substring(0, ExcerptLength) : p.Body),
                p.AuthorDisplayName))
            .ToListAsync(cancellationToken);

        return new PagedResult<PublicServiceItemSummaryDto>(posts, totalCount, normalizedPage, normalizedPageSize);
    }

    public async Task<PublicServiceItemDetailDto?> GetPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var normalized = slug.Trim().ToLowerInvariant();
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

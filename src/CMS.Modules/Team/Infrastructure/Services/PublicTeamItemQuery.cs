using CMS.Application.Common.Paging;
using CMS.Modules.Team.Application.Interfaces;
using CMS.Modules.Team.Application.TeamItems;
using CMS.Modules.Team.Domain.Enums;
using CMS.Modules.Team.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Team.Infrastructure.Services;

public sealed class PublicTeamItemQuery : IPublicTeamItemQuery
{
    public const int DefaultPageSize = 12;
    private const int ExcerptLength = 180;
    private readonly TeamDbContext _db;

    public PublicTeamItemQuery(TeamDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PublicTeamItemSummaryDto>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        var page = await ListPublishedPagedAsync(1, DefaultPageSize, cancellationToken);
        return page.Items;
    }

    public async Task<PagedResult<PublicTeamItemSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, 48);

        var query = _db.TeamItems
            .AsNoTracking()
            .Where(p => p.Status == TeamStatus.Published);

        var totalCount = await query.CountAsync(cancellationToken);

        var posts = await query
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(p => new PublicTeamItemSummaryDto(
                p.Title,
                p.Subtitle,
                p.Slug,
                p.Category != null ? p.Category.Name : null,
                p.CoverImageUrl,
                p.AvatarImageUrl,
                p.PublishedAtUtc ?? p.CreatedAtUtc,
                p.Excerpt ?? string.Empty,
                p.AuthorDisplayName))
            .ToListAsync(cancellationToken);

        return new PagedResult<PublicTeamItemSummaryDto>(posts, totalCount, normalizedPage, normalizedPageSize);
    }

    public async Task<PublicTeamItemDetailDto?> GetPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var normalized = slug.Trim().ToLowerInvariant();
        var post = await _db.TeamItems
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(
                p => p.Slug == normalized && p.Status == TeamStatus.Published,
                cancellationToken);

        if (post is null)
            return null;

        return new PublicTeamItemDetailDto(
            post.Id,
            post.Title,
            post.Subtitle,
            post.Slug,
            post.Body,
            post.Category?.Name,
            post.CoverImageUrl,
            post.AvatarImageUrl,
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

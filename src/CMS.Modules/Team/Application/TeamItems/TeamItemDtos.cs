using CMS.Application.Common.Paging;
using CMS.Modules.Team.Domain.Enums;

namespace CMS.Modules.Team.Application.TeamItems;

public sealed class TeamItemListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
    public string? Search { get; init; }
    public TeamStatus? Status { get; init; }
    public Guid? CategoryId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public string? Sort { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize =>
        PageSize < 1 ? PagedRequest.DefaultPageSize : Math.Min(PageSize, PagedRequest.MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

public sealed record TeamItemListItemDto(
    Guid Id,
    string Title,
    string Slug,
    TeamStatus Status,
    string? CategoryName,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc);

public sealed record TeamItemDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? Excerpt,
    TeamStatus Status,
    string? CoverImageUrl,
    Guid? CategoryId,
    string? AuthorUserId,
    string? AuthorDisplayName,
    DateTime? PublishedAtUtc,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    DateTime CreatedAtUtc);

public static class TeamItemDraftDefaults
{
    public const string Title = "پیش‌نویس";
}

public sealed record SaveTeamItemCommand(
    string Title,
    string? Slug,
    string Body,
    string? Excerpt,
    Guid? CategoryId,
    string? CoverImageUrl,
    bool Publish,
    DateTime? PublishedAtUtc,
    string? AuthorUserId,
    string? AuthorDisplayName,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl);

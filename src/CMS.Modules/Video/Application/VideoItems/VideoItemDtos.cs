using CMS.Application.Common.Paging;
using CMS.Modules.Video.Domain.Enums;

namespace CMS.Modules.Video.Application.VideoItems;

public sealed class VideoItemListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
    public string? Search { get; init; }
    public VideoStatus? Status { get; init; }
    public Guid? CategoryId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public string? Sort { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize =>
        PageSize < 1 ? PagedRequest.DefaultPageSize : Math.Min(PageSize, PagedRequest.MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

public sealed record VideoItemListItemDto(
    Guid Id,
    string Title,
    string Slug,
    VideoStatus Status,
    string? CategoryName,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc);

public sealed record VideoItemDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? Excerpt,
    string? VideoUrl,
    VideoStatus Status,
    string? CoverImageUrl,
    string? CoverImageAlt,
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

public static class VideoItemDraftDefaults
{
    public const string Title = "پیش‌نویس";
}

public sealed record SaveVideoItemCommand(
    string Title,
    string? Slug,
    string Body,
    string? Excerpt,
    string? VideoUrl,
    Guid? CategoryId,
    string? CoverImageUrl,
    string? CoverImageAlt,
    bool? Publish,
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

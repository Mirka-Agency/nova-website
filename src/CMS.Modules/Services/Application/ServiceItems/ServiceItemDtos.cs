using CMS.Application.Common.Paging;
using CMS.Modules.Services.Domain.Enums;

namespace CMS.Modules.Services.Application.ServiceItems;

public sealed class ServiceItemListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
    public string? Search { get; init; }
    public ServiceStatus? Status { get; init; }
    public Guid? CategoryId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public string? Sort { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize =>
        PageSize < 1 ? PagedRequest.DefaultPageSize : Math.Min(PageSize, PagedRequest.MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

public sealed record ServiceItemListItemDto(
    Guid Id,
    string Title,
    string Slug,
    ServiceStatus Status,
    string? CategoryName,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc);

public sealed record ServiceItemDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? Excerpt,
    ServiceStatus Status,
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

public static class ServiceItemDraftDefaults
{
    public const string Title = "پیش‌نویس";
}

public sealed record SaveServiceItemCommand(
    string Title,
    string? Slug,
    string Body,
    string? Excerpt,
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

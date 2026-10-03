using CMS.Application.Common.Paging;

namespace CMS.Modules.Honors.Application.HonorItems;

public sealed class HonorItemListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
    public string? Search { get; init; }
    public bool? IsPublished { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize =>
        PageSize < 1 ? PagedRequest.DefaultPageSize : Math.Min(PageSize, PagedRequest.MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

public sealed record HonorItemListItemDto(
    Guid Id,
    string Title,
    string ImageUrl,
    string? AltText,
    int SortOrder,
    bool IsPublished,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record HonorItemDetailDto(
    Guid Id,
    string Title,
    string ImageUrl,
    string? AltText,
    int SortOrder,
    bool IsPublished,
    DateTime? PublishedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record PublicHonorItemDto(
    Guid Id,
    string Title,
    string ImageUrl,
    string? AltText,
    int SortOrder);

public sealed record SaveHonorItemCommand(
    string Title,
    string ImageUrl,
    string? AltText,
    int SortOrder,
    bool IsPublished);

using CMS.Application.Common.Paging;

namespace CMS.Modules.Voices.Application.VoiceItems;

public sealed class VoiceItemListRequest
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

public sealed record VoiceItemListItemDto(
    Guid Id,
    string CustomerName,
    string? Subtitle,
    string AudioUrl,
    int SortOrder,
    bool IsPublished,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record VoiceItemDetailDto(
    Guid Id,
    string CustomerName,
    string? Subtitle,
    string? Description,
    string AudioUrl,
    int SortOrder,
    bool IsPublished,
    DateTime? PublishedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record PublicVoiceItemDto(
    Guid Id,
    string CustomerName,
    string? Subtitle,
    string? Description,
    string AudioUrl,
    int SortOrder);

public sealed record SaveVoiceItemCommand(
    string CustomerName,
    string? Subtitle,
    string? Description,
    string AudioUrl,
    int SortOrder,
    bool IsPublished);

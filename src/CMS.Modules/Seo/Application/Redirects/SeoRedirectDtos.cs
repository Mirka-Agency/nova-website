using CMS.Application.Common.Paging;

namespace CMS.Modules.Seo.Application.Redirects;

public sealed class SeoRedirectListRequest
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;
    public string? Search { get; init; }
    public bool? IsActive { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedPageSize => PageSize < 1 ? DefaultPageSize : Math.Min(PageSize, MaxPageSize);
    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

public sealed record SeoRedirectListItemDto(
    Guid Id,
    string FromPath,
    string ToUrl,
    int StatusCode,
    bool IsActive,
    string? Note,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record SeoRedirectDetailDto(
    Guid Id,
    string FromPath,
    string ToUrl,
    int StatusCode,
    bool IsActive,
    string? Note,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record SeoRedirectMatchDto(
    string ToUrl,
    int StatusCode);

public sealed record SaveSeoRedirectCommand(
    string FromPath,
    string ToUrl,
    int StatusCode,
    bool IsActive,
    string? Note);

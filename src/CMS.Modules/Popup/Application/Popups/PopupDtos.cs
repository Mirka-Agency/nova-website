using CMS.Application.Common.Paging;
using CMS.Modules.Popup.Domain.Enums;

namespace CMS.Modules.Popup.Application.Popups;

public sealed class PopupListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
    public string? Search { get; init; }
    public bool? IsActive { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize =>
        PageSize < 1 ? PagedRequest.DefaultPageSize : Math.Min(PageSize, PagedRequest.MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

public sealed record PopupListItemDto(
    Guid Id,
    string Title,
    string Slug,
    bool IsActive,
    string TriggerType,
    PopupFrequency Frequency,
    PopupPageTargetMode PageTargetMode,
    Guid? FormId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record PopupOptionDto(Guid Id, string Title, string Slug);

public sealed record PopupDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string BodyText,
    string? ImageUrl,
    string ContentHtml,
    bool IsActive,
    Guid? FormId,
    string CtaText,
    string CtaAction,
    string? CtaUrl,
    Guid? CtaTargetPopupId,
    string TriggerType,
    int? TriggerDelaySeconds,
    int? TriggerScrollPercent,
    string? TriggerSelector,
    string? TriggerConfigJson,
    bool ShowOverlay,
    bool ShowCloseButton,
    bool CloseOnOverlayClick,
    bool CloseOnEscape,
    bool LockBodyScroll,
    bool EnableContentScroll,
    PopupFrequency Frequency,
    PopupPageTargetMode PageTargetMode,
    string PagePaths,
    string? ExtensionSettingsJson,
    int SortOrder,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record PublicPopupDto(
    Guid Id,
    string Title,
    string Slug,
    string BodyText,
    string? ImageUrl,
    string ContentHtml,
    Guid? FormId,
    string CtaText,
    string CtaAction,
    string? CtaUrl,
    Guid? CtaTargetPopupId,
    string? CtaTargetPopupSlug,
    string TriggerType,
    int? TriggerDelaySeconds,
    int? TriggerScrollPercent,
    string? TriggerSelector,
    string? TriggerConfigJson,
    bool ShowOverlay,
    bool ShowCloseButton,
    bool CloseOnOverlayClick,
    bool CloseOnEscape,
    bool LockBodyScroll,
    bool EnableContentScroll,
    PopupFrequency Frequency,
    string? ExtensionSettingsJson,
    int SortOrder);

public sealed record SavePopupCommand(
    string Title,
    string? Slug,
    string? BodyText,
    string? ImageUrl,
    string? ContentHtml,
    bool IsActive,
    Guid? FormId,
    string? CtaText,
    string? CtaAction,
    string? CtaUrl,
    Guid? CtaTargetPopupId,
    string TriggerType,
    int? TriggerDelaySeconds,
    int? TriggerScrollPercent,
    string? TriggerSelector,
    string? TriggerConfigJson,
    bool ShowOverlay,
    bool ShowCloseButton,
    bool CloseOnOverlayClick,
    bool CloseOnEscape,
    bool LockBodyScroll,
    bool EnableContentScroll,
    PopupFrequency Frequency,
    PopupPageTargetMode PageTargetMode,
    string? PagePaths,
    string? ExtensionSettingsJson,
    int SortOrder = 0);

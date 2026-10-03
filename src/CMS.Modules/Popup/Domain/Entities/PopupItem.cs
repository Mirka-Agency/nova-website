using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Popup.Domain.Enums;

namespace CMS.Modules.Popup.Domain.Entities;

public class PopupItem : BaseEntity
{
    private PopupItem()
    {
    }

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    /// <summary>Plain body text for non-technical editors.</summary>
    public string BodyText { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    /// <summary>Optional legacy/custom HTML (advanced).</summary>
    public string ContentHtml { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public Guid? FormId { get; private set; }

    public string CtaText { get; private set; } = string.Empty;
    public string CtaAction { get; private set; } = PopupCtaActions.None;
    public string? CtaUrl { get; private set; }
    public Guid? CtaTargetPopupId { get; private set; }

    public string TriggerType { get; private set; } = PopupTriggerTypes.Manual;
    public int? TriggerDelaySeconds { get; private set; }
    public int? TriggerScrollPercent { get; private set; }
    public string? TriggerSelector { get; private set; }
    public string? TriggerConfigJson { get; private set; }

    public bool ShowOverlay { get; private set; } = true;
    public bool ShowCloseButton { get; private set; } = true;
    public bool CloseOnOverlayClick { get; private set; } = true;
    public bool CloseOnEscape { get; private set; } = true;
    public bool LockBodyScroll { get; private set; } = true;
    public bool EnableContentScroll { get; private set; } = true;

    public PopupFrequency Frequency { get; private set; } = PopupFrequency.OncePerBrowser;
    public PopupPageTargetMode PageTargetMode { get; private set; } = PopupPageTargetMode.All;
    public string PagePaths { get; private set; } = string.Empty;

    public string? ExtensionSettingsJson { get; private set; }
    public int SortOrder { get; private set; }

    public static PopupItem Create(PopupContent content, PopupBehavior behavior, int sortOrder = 0)
    {
        var item = new PopupItem();
        item.Apply(content, behavior, sortOrder);
        return item;
    }

    public void Update(PopupContent content, PopupBehavior behavior, int sortOrder)
    {
        Apply(content, behavior, sortOrder);
        Touch();
    }

    public void SetActive(bool isActive)
    {
        if (IsActive == isActive)
            return;
        IsActive = isActive;
        Touch();
    }

    private void Apply(PopupContent content, PopupBehavior behavior, int sortOrder)
    {
        Validate(content, behavior);

        Title = content.Title.Trim();
        Slug = content.Slug.Trim().ToLowerInvariant();
        BodyText = content.BodyText?.Trim() ?? string.Empty;
        ImageUrl = string.IsNullOrWhiteSpace(content.ImageUrl) ? null : content.ImageUrl.Trim();
        ContentHtml = content.ContentHtml?.Trim() ?? string.Empty;
        IsActive = content.IsActive;
        FormId = content.FormId;

        var ctaAction = PopupCtaActions.Normalize(content.CtaAction);
        CtaAction = ctaAction;
        CtaText = ctaAction == PopupCtaActions.None ? string.Empty : (content.CtaText?.Trim() ?? string.Empty);
        CtaUrl = ctaAction == PopupCtaActions.Url && !string.IsNullOrWhiteSpace(content.CtaUrl)
            ? content.CtaUrl.Trim()
            : null;
        CtaTargetPopupId = ctaAction == PopupCtaActions.OpenPopup ? content.CtaTargetPopupId : null;

        var normalizedTrigger = content.TriggerType.Trim().ToLowerInvariant();
        TriggerType = normalizedTrigger;
        TriggerDelaySeconds = normalizedTrigger == PopupTriggerTypes.Timer ? content.TriggerDelaySeconds : null;
        TriggerScrollPercent = normalizedTrigger == PopupTriggerTypes.Scroll ? content.TriggerScrollPercent : null;
        TriggerSelector = string.IsNullOrWhiteSpace(content.TriggerSelector) ? null : content.TriggerSelector.Trim();
        TriggerConfigJson = string.IsNullOrWhiteSpace(content.TriggerConfigJson) ? null : content.TriggerConfigJson.Trim();

        ShowOverlay = behavior.ShowOverlay;
        ShowCloseButton = behavior.ShowCloseButton;
        CloseOnOverlayClick = behavior.CloseOnOverlayClick;
        CloseOnEscape = behavior.CloseOnEscape;
        LockBodyScroll = behavior.LockBodyScroll;
        EnableContentScroll = behavior.EnableContentScroll;
        Frequency = behavior.Frequency;
        PageTargetMode = behavior.PageTargetMode;
        PagePaths = NormalizePaths(behavior.PagePaths);
        ExtensionSettingsJson = string.IsNullOrWhiteSpace(behavior.ExtensionSettingsJson)
            ? null
            : behavior.ExtensionSettingsJson.Trim();
        SortOrder = sortOrder;
    }

    private static void Validate(PopupContent content, PopupBehavior behavior)
    {
        if (string.IsNullOrWhiteSpace(content.Title))
            throw new DomainException("عنوان پاپ‌آپ الزامی است.");
        if (content.Title.Trim().Length > 200)
            throw new DomainException("عنوان پاپ‌آپ خیلی طولانی است.");
        if (string.IsNullOrWhiteSpace(content.Slug))
            throw new DomainException("نامک پاپ‌آپ الزامی است.");
        if (content.Slug.Trim().Length > 200)
            throw new DomainException("نامک پاپ‌آپ خیلی طولانی است.");
        if (string.IsNullOrWhiteSpace(content.TriggerType))
            throw new DomainException("نوع تریگر الزامی است.");

        var trigger = content.TriggerType.Trim().ToLowerInvariant();
        if (trigger == PopupTriggerTypes.Timer)
        {
            if (content.TriggerDelaySeconds is null or < 0 or > 3600)
                throw new DomainException("تأخیر تایمر باید بین ۰ تا ۳۶۰۰ ثانیه باشد.");
        }

        if (trigger == PopupTriggerTypes.Scroll)
        {
            if (content.TriggerScrollPercent is null or < 1 or > 100)
                throw new DomainException("درصد اسکرول باید بین ۱ تا ۱۰۰ باشد.");
        }

        if (behavior.PageTargetMode is PopupPageTargetMode.Include or PopupPageTargetMode.Exclude)
        {
            if (string.IsNullOrWhiteSpace(behavior.PagePaths))
                throw new DomainException("برای این حالت هدف‌گذاری، حداقل یک مسیر صفحه لازم است.");
        }

        var cta = PopupCtaActions.Normalize(content.CtaAction);
        if (cta != PopupCtaActions.None && string.IsNullOrWhiteSpace(content.CtaText))
            throw new DomainException("متن دکمه الزامی است.");
        if (cta == PopupCtaActions.Url && string.IsNullOrWhiteSpace(content.CtaUrl))
            throw new DomainException("آدرس لینک دکمه الزامی است.");
        if (cta == PopupCtaActions.OpenPopup && content.CtaTargetPopupId is null)
            throw new DomainException("پاپ‌آپ مقصد را انتخاب کنید.");
    }

    private static string NormalizePaths(string? pagePaths)
    {
        if (string.IsNullOrWhiteSpace(pagePaths))
            return string.Empty;

        var lines = pagePaths
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.StartsWith('/') ? p : "/" + p)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join('\n', lines);
    }
}

public sealed record PopupContent(
    string Title,
    string Slug,
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
    string? TriggerConfigJson);

public sealed record PopupBehavior(
    bool ShowOverlay,
    bool ShowCloseButton,
    bool CloseOnOverlayClick,
    bool CloseOnEscape,
    bool LockBodyScroll,
    bool EnableContentScroll,
    PopupFrequency Frequency,
    PopupPageTargetMode PageTargetMode,
    string? PagePaths,
    string? ExtensionSettingsJson);

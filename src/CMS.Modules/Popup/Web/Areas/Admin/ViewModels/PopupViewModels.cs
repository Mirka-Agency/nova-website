using System.ComponentModel.DataAnnotations;
using CMS.Modules.Popup.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CMS.Modules.Popup.Web.Areas.Admin.ViewModels;

public sealed class PopupIndexViewModel
{
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public int Page { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public IReadOnlyList<PopupListItemViewModel> Items { get; init; } = [];
}

public sealed class PopupListItemViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public string TriggerLabel { get; set; } = string.Empty;
    public string FrequencyLabel { get; set; } = string.Empty;
    public string PageTargetLabel { get; set; } = string.Empty;
    public bool HasForm { get; set; }
}

public sealed class PopupFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "عنوان الزامی است.")]
    [MaxLength(200, ErrorMessage = "عنوان حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "نامک حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string? Slug { get; set; }

    [MaxLength(20000, ErrorMessage = "متن خیلی طولانی است.")]
    public string? BodyText { get; set; }

    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    public string? ContentHtml { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? FormId { get; set; }

    [MaxLength(120)]
    public string? CtaText { get; set; }

    [MaxLength(32)]
    public string CtaAction { get; set; } = PopupCtaActions.None;

    [MaxLength(1000)]
    public string? CtaUrl { get; set; }

    public Guid? CtaTargetPopupId { get; set; }

    [Required(ErrorMessage = "نوع تریگر الزامی است.")]
    [MaxLength(64)]
    public string TriggerType { get; set; } = PopupTriggerTypes.Manual;

    [Range(0, 3600, ErrorMessage = "تأخیر باید بین ۰ تا ۳۶۰۰ ثانیه باشد.")]
    public int? TriggerDelaySeconds { get; set; } = 5;

    [Range(1, 100, ErrorMessage = "درصد اسکرول باید بین ۱ تا ۱۰۰ باشد.")]
    public int? TriggerScrollPercent { get; set; } = 50;

    [MaxLength(300)]
    public string? TriggerSelector { get; set; }

    public bool ShowOverlay { get; set; } = true;
    public bool ShowCloseButton { get; set; } = true;
    public bool CloseOnOverlayClick { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;
    public bool LockBodyScroll { get; set; } = true;
    public bool EnableContentScroll { get; set; } = true;

    public PopupFrequency Frequency { get; set; } = PopupFrequency.OncePerBrowser;
    public PopupPageTargetMode PageTargetMode { get; set; } = PopupPageTargetMode.All;

    [MaxLength(8000)]
    public string? PagePaths { get; set; }

    public int SortOrder { get; set; }

    public IReadOnlyList<SelectListItem> FormOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> CtaActionOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> PopupTargetOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> FrequencyOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> PageTargetOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> DelaySecondsOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> ScrollPercentOptions { get; set; } = [];
}

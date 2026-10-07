using System.ComponentModel.DataAnnotations;
using CMS.Modules.Forms.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CMS.Modules.Forms.Web.Areas.Admin.ViewModels;

public sealed class FormIndexViewModel
{
    public IReadOnlyList<FormListItemViewModel> Items { get; init; } = [];
    public string? Search { get; init; }
    public FormStatus? Status { get; init; }
    public string? Sort { get; init; }
    public int Page { get; init; } = 1;
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}

public sealed class FormListItemViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public FormStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public int FieldCount { get; init; }
    public int SubmissionCount { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public bool IsSystem { get; init; }
}

public sealed class FormFormViewModel
{
    public Guid? Id { get; set; }

    /// <summary>general | fields | submit | actions | security | advanced | responses</summary>
    public string ActiveTab { get; set; } = "general";

    public int SubmissionCount { get; set; }

    public bool IsSystem { get; set; }

    [Required(ErrorMessage = "نام فرم الزامی است.")]
    [StringLength(200, ErrorMessage = "نام فرم حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "کلید فرم حداکثر ۱۰۰ نویسه می‌تواند باشد.")]
    [RegularExpression(@"^[A-Za-z0-9_-]*$", ErrorMessage = "کلید فرم فقط می‌تواند شامل حرف، عدد، خط زیر یا خط تیره باشد.")]
    public string? Key { get; set; }

    [StringLength(200, ErrorMessage = "نامک حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string? Slug { get; set; }

    [StringLength(2000, ErrorMessage = "توضیحات خیلی طولانی است.")]
    public string? Description { get; set; }

    public FormStatus Status { get; set; } = FormStatus.Draft;

    /// <summary>When true, form is publicly viewable/submittable (Status = Published).</summary>
    public bool Publish { get; set; }

    [StringLength(1000)]
    public string? SuccessMessage { get; set; }

    [StringLength(1000)]
    public string? RedirectUrl { get; set; }

    /// <summary>message | redirect | page</summary>
    public string SubmitBehaviorType { get; set; } = "message";

    [StringLength(100)]
    public string SubmitButtonText { get; set; } = "ارسال";

    public bool SendEmailNotification { get; set; }

    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    [StringLength(256, ErrorMessage = "ایمیل حداکثر ۲۵۶ نویسه می‌تواند باشد.")]
    public string? NotifyEmail { get; set; }

    [StringLength(300)]
    public string? NotifyEmailSubject { get; set; }

    [StringLength(200)]
    public string? NotifySenderName { get; set; }

    [StringLength(100)]
    public string? NotifyReplyToFieldKey { get; set; }

    public Guid? NotifyReplyToFieldId { get; set; }

    public bool AutoReplyEnabled { get; set; }

    [StringLength(300)]
    public string? AutoReplySubject { get; set; }

    [StringLength(4000)]
    public string? AutoReplyBody { get; set; }

    [StringLength(100)]
    public string? AutoReplyEmailFieldKey { get; set; }

    public Guid? AutoReplyEmailFieldId { get; set; }

    public bool WebhookEnabled { get; set; }

    [StringLength(2000)]
    public string? WebhookUrl { get; set; }

    [StringLength(500)]
    public string? WebhookSecret { get; set; }

    public bool SendWhatsAppNotification { get; set; }

    [StringLength(200)]
    public string? WhatsAppGroupId { get; set; }

    [StringLength(300)]
    public string? WhatsAppGroupName { get; set; }

    [StringLength(8000)]
    public string? WhatsAppTemplate { get; set; }

    public bool EnableCaptcha { get; set; }

    public bool AntiSpamEnabled { get; set; } = true;

    [StringLength(50)]
    public string AntiSpamProvider { get; set; } = "honeypot";

    [StringLength(500)]
    public string? AntiSpamSiteKey { get; set; }

    [StringLength(500)]
    public string? AntiSpamSecretKey { get; set; }

    [StringLength(200)]
    public string? SimpleCaptchaExpected { get; set; }

    public IReadOnlyList<FormFieldItemViewModel> Fields { get; set; } = [];
    public IReadOnlyList<FormVersionItemViewModel> Versions { get; set; } = [];
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = [];
    public IEnumerable<SelectListItem> EmailFieldOptions { get; set; } = [];
    public IEnumerable<SelectListItem> SubmitBehaviorOptions { get; set; } = [];
    public IEnumerable<SelectListItem> AntiSpamProviderOptions { get; set; } = [];
}

public sealed class FormVersionItemViewModel
{
    public Guid Id { get; init; }
    public int VersionNumber { get; init; }
    public string State { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public bool IsPublishedPointer { get; init; }
    public bool IsDraftPointer { get; init; }
}

public sealed class FormFieldItemViewModel
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public string? OptionsCsv { get; init; }
    public string? Placeholder { get; init; }
    public string? HelpText { get; init; }
    public int SortOrder { get; init; }
    public bool IsSystem { get; init; }
}

public sealed class FormFieldFormViewModel
{
    public Guid FormId { get; set; }

    public Guid? FieldId { get; set; }

    public bool IsSystem { get; set; }

    public bool FormIsSystem { get; set; }

    [StringLength(100, ErrorMessage = "کلید فیلد حداکثر ۱۰۰ نویسه می‌تواند باشد.")]
    public string? Key { get; set; }

    [Required(ErrorMessage = "برچسب فیلد الزامی است.")]
    [StringLength(200, ErrorMessage = "برچسب فیلد حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string Label { get; set; } = string.Empty;

    public FormFieldType FieldType { get; set; } = FormFieldType.Text;

    public bool IsRequired { get; set; }

    [StringLength(2000, ErrorMessage = "گزینه‌ها خیلی طولانی است.")]
    public string? OptionsCsv { get; set; }

    [StringLength(300)]
    public string? Placeholder { get; set; }

    [StringLength(1000)]
    public string? HelpText { get; set; }

    [StringLength(4000)]
    public string? SettingsJson { get; set; }

    [StringLength(1000)]
    public string? DefaultValue { get; set; }

    public string LayoutWidth { get; set; } = "full";

    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }

    [StringLength(500)]
    public string? Pattern { get; set; }

    [StringLength(500)]
    public string? AllowedExtensions { get; set; }

    public double? MaxFileSizeMb { get; set; }
    public int? MinSelections { get; set; }
    public int? MaxSelections { get; set; }

    /// <summary>always | all | any</summary>
    public string VisibilityMode { get; set; } = "always";

    public List<FormFieldVisibilityConditionRow> VisibilityConditions { get; set; } = [];

    [Range(0, 10_000, ErrorMessage = "ترتیب نمایش نامعتبر است.")]
    public int SortOrder { get; set; }

    public IEnumerable<SelectListItem> FieldTypeOptions { get; set; } = [];
    public IEnumerable<SelectListItem> LayoutWidthOptions { get; set; } = [];
    public IEnumerable<SelectListItem> VisibilityModeOptions { get; set; } = [];
    public IEnumerable<SelectListItem> VisibilityOperatorOptions { get; set; } = [];
    public IEnumerable<SelectListItem> VisibilityFieldOptions { get; set; } = [];
}

public sealed class FormFieldVisibilityConditionRow
{
    public string? FieldId { get; set; }

    /// <summary>equals | not_equals | contains | is_empty | is_not_empty</summary>
    public string Operator { get; set; } = "equals";

    [StringLength(1000)]
    public string? Value { get; set; }
}

public sealed class SubmissionIndexViewModel
{
    public IReadOnlyList<SubmissionListItemViewModel> Items { get; init; } = [];
    public Guid? FormId { get; init; }
    public string? FormName { get; init; }
    public string? Search { get; init; }
    public SubmissionStatus? Status { get; init; }
    public string? FromLocal { get; init; }
    public string? ToLocal { get; init; }
    public int Page { get; init; } = 1;
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}

public sealed class SubmissionListItemViewModel
{
    public Guid Id { get; init; }
    public Guid FormId { get; init; }
    public string FormName { get; init; } = string.Empty;
    public DateTime SubmittedAtUtc { get; init; }
    public SubmissionStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public string? Preview { get; init; }
    public string? IpAddress { get; init; }
}

public sealed class SubmissionDetailViewModel
{
    public Guid Id { get; init; }
    public Guid FormId { get; init; }
    public Guid FormVersionId { get; init; }
    public string FormName { get; init; } = string.Empty;
    public DateTime SubmittedAtUtc { get; init; }
    public SubmissionStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? Page { get; init; }
    public string? Locale { get; init; }
    public string? Referrer { get; init; }
    public string? UtmSource { get; init; }
    public string? UtmMedium { get; init; }
    public string? UtmCampaign { get; init; }
    public string? UtmTerm { get; init; }
    public string? UtmContent { get; init; }
    public IReadOnlyList<SubmissionValueViewModel> Values { get; init; } = [];
    public IReadOnlyList<SubmissionFileViewModel> Files { get; init; } = [];
}

public sealed class SubmissionValueViewModel
{
    public string FieldKey { get; init; } = string.Empty;
    public string? FieldLabel { get; init; }
    public string? FieldType { get; init; }
    public string? Value { get; init; }
}

public sealed class SubmissionFileViewModel
{
    public Guid Id { get; init; }
    public string FieldKey { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string? PublicUrl { get; init; }
}

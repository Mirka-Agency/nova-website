using CMS.Application.Common.Paging;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Application.Forms;

public sealed class FormListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
    public string? Search { get; init; }
    public FormStatus? Status { get; init; }
    public string? Sort { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize =>
        PageSize < 1 ? PagedRequest.DefaultPageSize : Math.Min(PageSize, PagedRequest.MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

public sealed record FormListItemDto(
    Guid Id,
    string Name,
    string Key,
    string Slug,
    FormStatus Status,
    bool IsPublished,
    int FieldCount,
    int SubmissionCount,
    DateTime CreatedAtUtc,
    Guid? PublishedVersionId,
    Guid? DraftVersionId,
    bool IsSystem = false);

public sealed record FormFieldDto(
    Guid Id,
    string Key,
    string Label,
    FormFieldType FieldType,
    bool IsRequired,
    string? OptionsCsv,
    string? Placeholder,
    string? HelpText,
    string? SettingsJson,
    int SortOrder,
    string? LayoutWidth = null,
    string? DefaultValue = null,
    bool IsSystem = false);

public sealed record FormDetailDto(
    Guid Id,
    string Name,
    string Key,
    string Slug,
    string? Description,
    FormStatus Status,
    bool IsPublished,
    Guid? PublishedVersionId,
    Guid? DraftVersionId,
    string? SuccessMessage,
    string? RedirectUrl,
    string SubmitBehaviorType,
    string SubmitButtonText,
    bool SendEmailNotification,
    string? NotifyEmail,
    string? NotifyEmailSubject,
    string? NotifySenderName,
    string? NotifyReplyToFieldKey,
    Guid? NotifyReplyToFieldId,
    bool AutoReplyEnabled,
    string? AutoReplySubject,
    string? AutoReplyBody,
    string? AutoReplyEmailFieldKey,
    Guid? AutoReplyEmailFieldId,
    bool WebhookEnabled,
    string? WebhookUrl,
    string? WebhookSecret,
    bool EnableCaptcha,
    bool AntiSpamEnabled,
    string AntiSpamProvider,
    string? AntiSpamSiteKey,
    string? AntiSpamSecretKey,
    string? SimpleCaptchaExpected,
    IReadOnlyList<FormFieldDto> Fields,
    IReadOnlyList<FormVersionItemDto> Versions,
    bool IsSystem = false);

public sealed record FormVersionItemDto(
    Guid Id,
    int VersionNumber,
    FormVersionState State,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    bool IsPublishedPointer,
    bool IsDraftPointer);

public sealed record SaveFormCommand(
    string Name,
    string? Key,
    string? Slug,
    string? Description,
    FormStatus Status,
    string? SuccessMessage,
    string? RedirectUrl,
    string? SubmitButtonText,
    bool SendEmailNotification,
    string? NotifyEmail,
    string? NotifyEmailSubject,
    string? NotifySenderName,
    string? NotifyReplyToFieldKey,
    Guid? NotifyReplyToFieldId,
    bool AutoReplyEnabled,
    string? AutoReplySubject,
    string? AutoReplyBody,
    string? AutoReplyEmailFieldKey,
    Guid? AutoReplyEmailFieldId,
    bool EnableCaptcha,
    string SubmitBehaviorType = "message",
    bool WebhookEnabled = false,
    string? WebhookUrl = null,
    string? WebhookSecret = null,
    bool AntiSpamEnabled = true,
    string AntiSpamProvider = "honeypot",
    string? AntiSpamSiteKey = null,
    string? AntiSpamSecretKey = null,
    string? SimpleCaptchaExpected = null);

public sealed record SaveFormFieldCommand(
    string Key,
    string Label,
    FormFieldType FieldType,
    bool IsRequired,
    string? OptionsCsv,
    string? Placeholder,
    string? HelpText,
    string? SettingsJson,
    int SortOrder,
    string? DefaultValue = null,
    string? LayoutWidth = null,
    int? MinLength = null,
    int? MaxLength = null,
    decimal? Min = null,
    decimal? Max = null,
    string? Pattern = null,
    string? AllowedExtensions = null,
    double? MaxFileSizeMb = null,
    int? MinSelections = null,
    int? MaxSelections = null,
    FormFieldVisibilitySchema? Visibility = null);

public sealed record FormTemplateInfoDto(
    FormTemplateKind Kind,
    string Name,
    string Description);

public sealed record ReorderFieldsCommand(IReadOnlyList<Guid> OrderedFieldIds);

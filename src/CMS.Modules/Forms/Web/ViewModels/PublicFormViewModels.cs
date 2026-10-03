using CMS.Modules.Forms.Domain.Enums;
using CMS.Modules.Forms.Application.Public;

namespace CMS.Modules.Forms.Web.ViewModels;

public sealed class PublicFormSubmitViewModel
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? PublishedVersionId { get; set; }
    public string SubmitButtonText { get; set; } = "ارسال";
    public string SubmitBehaviorType { get; set; } = "message";
    public string? SuccessMessage { get; set; }
    public string? RedirectUrl { get; set; }
    public bool EnableCaptcha { get; set; }
    public bool AntiSpamEnabled { get; set; } = true;
    public string AntiSpamProvider { get; set; } = "honeypot";
    public string? AntiSpamSiteKey { get; set; }

    /// <summary>Relative schema JSON URL for themes (<c>/forms/{slug}/schema</c>).</summary>
    public string SchemaUrl { get; set; } = string.Empty;

    public IReadOnlyList<PublicFormFieldViewModel> Fields { get; set; } = [];
    public Dictionary<string, string?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Honeypot — must stay empty.</summary>
    public string? Website { get; set; }

    public string? CaptchaAnswer { get; set; }

    /// <summary>Token from external challenge providers (Turnstile / reCAPTCHA / hCaptcha).</summary>
    public string? AntiSpamToken { get; set; }
}

public sealed class PublicFormFieldViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;

    /// <summary>Schema type id (snake_case) — preferred for theme rendering.</summary>
    public string TypeId { get; init; } = "text";

    /// <summary>Legacy enum; prefer <see cref="TypeId"/>.</summary>
    public FormFieldType FieldType { get; init; }

    public bool IsRequired { get; init; }
    public string? Placeholder { get; init; }
    public string? HelpText { get; init; }
    public string? SettingsJson { get; init; }
    public string? LayoutWidth { get; init; }
    public string? DefaultValue { get; init; }
    public IReadOnlyList<string>? AllowedExtensions { get; init; }
    public long? MaxFileSizeBytes { get; init; }
    public IReadOnlyList<PublicFormOptionViewModel> Options { get; init; } = [];
    public string? Value { get; set; }
    public FormPublicVisibility? Visibility { get; init; }
}

public sealed class PublicFormOptionViewModel
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}

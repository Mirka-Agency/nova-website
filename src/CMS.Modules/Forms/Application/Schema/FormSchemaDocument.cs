using System.Text.Json.Serialization;

namespace CMS.Modules.Forms.Application.Schema;

/// <summary>
/// Theme-independent form schema stored in <c>FormVersion.SchemaJson</c>.
/// Source of truth for fields, submitBehavior, actions, antiSpam, and settings.
/// </summary>
public sealed class FormSchemaDocument
{
    public const int CurrentSchemaVersion = 1;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    [JsonPropertyName("fields")]
    public IReadOnlyList<FormSchemaField> Fields { get; init; } = [];

    [JsonPropertyName("submitBehavior")]
    public FormSubmitBehaviorSchema SubmitBehavior { get; init; } = new() { Type = "message" };

    [JsonPropertyName("actions")]
    public IReadOnlyList<FormActionSchema> Actions { get; init; } = [];

    [JsonPropertyName("antiSpam")]
    public FormAntiSpamSchema AntiSpam { get; init; } = new();

    [JsonPropertyName("settings")]
    public FormSettingsSchema Settings { get; init; } = new();
}

public sealed class FormSchemaField
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; init; }

    [JsonPropertyName("helpText")]
    public string? HelpText { get; init; }

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; init; }

    [JsonPropertyName("validation")]
    public FormFieldValidationSchema Validation { get; init; } = new();

    [JsonPropertyName("options")]
    public IReadOnlyList<FormFieldOptionSchema> Options { get; init; } = [];

    [JsonPropertyName("visibility")]
    public FormFieldVisibilitySchema? Visibility { get; init; }

    [JsonPropertyName("layout")]
    public FormFieldLayoutSchema? Layout { get; init; }

    [JsonPropertyName("position")]
    public int Position { get; init; }

    [JsonPropertyName("settings")]
    public IReadOnlyDictionary<string, string>? Settings { get; init; }
}

public sealed class FormFieldValidationSchema
{
    [JsonPropertyName("required")]
    public bool? Required { get; init; }

    [JsonPropertyName("minLength")]
    public int? MinLength { get; init; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; init; }

    [JsonPropertyName("min")]
    public decimal? Min { get; init; }

    [JsonPropertyName("max")]
    public decimal? Max { get; init; }

    [JsonPropertyName("email")]
    public bool? Email { get; init; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; init; }

    [JsonPropertyName("allowedExtensions")]
    public IReadOnlyList<string>? AllowedExtensions { get; init; }

    [JsonPropertyName("maxFileSize")]
    public long? MaxFileSize { get; init; }

    [JsonPropertyName("minSelections")]
    public int? MinSelections { get; init; }

    [JsonPropertyName("maxSelections")]
    public int? MaxSelections { get; init; }
}

public sealed class FormFieldOptionSchema
{
    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;
}

public sealed class FormFieldVisibilitySchema
{
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = "all";

    [JsonPropertyName("conditions")]
    public IReadOnlyList<FormVisibilityConditionSchema> Conditions { get; init; } = [];
}

public sealed class FormVisibilityConditionSchema
{
    [JsonPropertyName("fieldId")]
    public string FieldId { get; init; } = string.Empty;

    [JsonPropertyName("operator")]
    public string Operator { get; init; } = "equals";

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

public sealed class FormFieldLayoutSchema
{
    [JsonPropertyName("width")]
    public string Width { get; init; } = "full";
}

public sealed class FormSubmitBehaviorSchema
{
    /// <summary>message | redirect | page</summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "message";

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("pageId")]
    public string? PageId { get; init; }
}

public sealed class FormActionSchema
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("config")]
    public IReadOnlyDictionary<string, object?> Config { get; init; }
        = new Dictionary<string, object?>();
}

public sealed class FormAntiSpamSchema
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    /// <summary>honeypot | turnstile | recaptcha | hcaptcha | simple_captcha</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; init; } = "honeypot";

    [JsonPropertyName("config")]
    public IReadOnlyDictionary<string, object?> Config { get; init; }
        = new Dictionary<string, object?>();
}

public sealed class FormSettingsSchema
{
    [JsonPropertyName("submitButtonText")]
    public string SubmitButtonText { get; init; } = "ارسال";
}

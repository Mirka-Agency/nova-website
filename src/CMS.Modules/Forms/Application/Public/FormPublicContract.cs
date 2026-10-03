namespace CMS.Modules.Forms.Application.Public;

/// <summary>
/// Theme-independent public form contract. Safe for JSON APIs and theme renderers.
/// Does not include admin-only action configs or anti-spam secrets.
/// </summary>
public sealed record FormPublicContract(
    int SchemaVersion,
    Guid Id,
    string Key,
    string Slug,
    string Name,
    string? Description,
    Guid? VersionId,
    string SubmitUrl,
    FormPublicSubmitBehavior SubmitBehavior,
    FormPublicSettings Settings,
    FormPublicAntiSpam AntiSpam,
    IReadOnlyList<FormPublicField> Fields);

public sealed record FormPublicSubmitBehavior(
    string Type,
    string? Message,
    string? Url,
    string? PageId);

public sealed record FormPublicSettings(string SubmitButtonText);

public sealed record FormPublicAntiSpam(
    bool Enabled,
    string Provider,
    string? SiteKey);

public sealed record FormPublicField(
    string Id,
    string Key,
    string Type,
    string Label,
    string? Placeholder,
    string? HelpText,
    bool Required,
    string? DefaultValue,
    string LayoutWidth,
    int Position,
    IReadOnlyList<FormPublicOption> Options,
    FormPublicFieldValidation? Validation = null,
    FormPublicVisibility? Visibility = null);

public sealed record FormPublicOption(string Value, string Label);

public sealed record FormPublicFieldValidation(
    int? MinLength,
    int? MaxLength,
    decimal? Min,
    decimal? Max,
    string? Pattern,
    IReadOnlyList<string>? AllowedExtensions,
    long? MaxFileSize,
    int? MinSelections,
    int? MaxSelections);

public sealed record FormPublicVisibility(
    string Mode,
    IReadOnlyList<FormPublicVisibilityCondition> Conditions);

public sealed record FormPublicVisibilityCondition(
    string FieldKey,
    string Operator,
    string? Value);

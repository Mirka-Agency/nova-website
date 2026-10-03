using System.Text.Json.Serialization;

namespace CMS.Modules.Forms.Application.Submissions;

/// <summary>Canonical submission payload stored in FormSubmission.DataJson.</summary>
public sealed class SubmissionDataDocument
{
    [JsonPropertyName("fields")]
    public IReadOnlyList<SubmissionDataField> Fields { get; init; } = [];
}

public sealed class SubmissionDataField
{
    [JsonPropertyName("fieldId")]
    public string? FieldId { get; init; }

    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    /// <summary>
    /// Human-readable label(s) for choice fields (select/radio/checkbox_group),
    /// when <see cref="Value"/> stores the machine option value.
    /// </summary>
    [JsonPropertyName("displayValue")]
    public string? DisplayValue { get; init; }

    /// <summary>When set, value is a file reference (see FormSubmissionFiles).</summary>
    [JsonPropertyName("fileId")]
    public string? FileId { get; init; }
}

public sealed class SubmissionContextDocument
{
    [JsonPropertyName("page")]
    public string? Page { get; init; }

    [JsonPropertyName("locale")]
    public string? Locale { get; init; }

    [JsonPropertyName("referrer")]
    public string? Referrer { get; init; }

    [JsonPropertyName("utm_source")]
    public string? UtmSource { get; init; }

    [JsonPropertyName("utm_medium")]
    public string? UtmMedium { get; init; }

    [JsonPropertyName("utm_campaign")]
    public string? UtmCampaign { get; init; }

    [JsonPropertyName("utm_term")]
    public string? UtmTerm { get; init; }

    [JsonPropertyName("utm_content")]
    public string? UtmContent { get; init; }
}

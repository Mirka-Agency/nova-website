using System.Text.Json;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;
using CMS.Modules.Forms.Domain.Fields;

namespace CMS.Modules.Forms.Application.Fields;

/// <summary>
/// Builds a schema field from the legacy FormField row (including SettingsJson extras).
/// </summary>
public static class FormFieldSchemaFactory
{
    public static FormSchemaField FromEntity(FormField field, IFormFieldTypeRegistry registry)
    {
        var typeId = registry.ToTypeId(field.FieldType);
        var extras = ParseExtras(field.SettingsJson);
        var options = FormFieldOptionParser.Parse(field.OptionsCsv)
            .Select(o => new FormFieldOptionSchema { Value = o.Value, Label = o.Label })
            .ToList();

        var validation = extras.Validation ?? new FormFieldValidationSchema();
        validation = new FormFieldValidationSchema
        {
            Required = validation.Required ?? field.IsRequired,
            MinLength = validation.MinLength,
            MaxLength = validation.MaxLength,
            Min = validation.Min,
            Max = validation.Max,
            Email = validation.Email ?? (field.FieldType == FormFieldType.Email ? true : null),
            Pattern = validation.Pattern,
            AllowedExtensions = validation.AllowedExtensions,
            MaxFileSize = validation.MaxFileSize,
            MinSelections = validation.MinSelections,
            MaxSelections = validation.MaxSelections
        };

        return new FormSchemaField
        {
            Id = field.Id.ToString("D"),
            Key = field.Key,
            Type = typeId,
            Label = field.Label,
            Placeholder = field.Placeholder,
            HelpText = field.HelpText,
            Required = field.IsRequired,
            DefaultValue = extras.DefaultValue,
            Validation = validation,
            Options = options,
            Visibility = extras.Visibility,
            Layout = extras.Layout ?? new FormFieldLayoutSchema { Width = "full" },
            Position = field.SortOrder,
            Settings = extras.RawSettings
        };
    }

    public static string? BuildSettingsJson(FormFieldFormExtras extras, string? legacyRaw = null)
    {
        var bag = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(extras.DefaultValue))
            bag["defaultValue"] = extras.DefaultValue;
        if (extras.LayoutWidth is "half" or "full")
            bag["layout"] = new Dictionary<string, string> { ["width"] = extras.LayoutWidth };
        if (extras.HasValidation)
        {
            var v = new Dictionary<string, object?>();
            if (extras.MinLength is not null) v["minLength"] = extras.MinLength;
            if (extras.MaxLength is not null) v["maxLength"] = extras.MaxLength;
            if (extras.Min is not null) v["min"] = extras.Min;
            if (extras.Max is not null) v["max"] = extras.Max;
            if (!string.IsNullOrWhiteSpace(extras.Pattern)) v["pattern"] = extras.Pattern;
            if (extras.MaxFileSizeMb is not null) v["maxFileSize"] = (long)(extras.MaxFileSizeMb.Value * 1024 * 1024);
            if (!string.IsNullOrWhiteSpace(extras.AllowedExtensions))
            {
                v["allowedExtensions"] = extras.AllowedExtensions
                    .Split(['|', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToArray();
            }
            if (extras.MinSelections is not null) v["minSelections"] = extras.MinSelections;
            if (extras.MaxSelections is not null) v["maxSelections"] = extras.MaxSelections;
            if (v.Count > 0)
                bag["validation"] = v;
        }

        if (!string.IsNullOrWhiteSpace(extras.CaptchaExpected))
            bag["expected"] = extras.CaptchaExpected;

        if (extras.Visibility?.Conditions is { Count: > 0 })
        {
            var mode = string.IsNullOrWhiteSpace(extras.Visibility.Mode)
                ? "all"
                : extras.Visibility.Mode.Trim().ToLowerInvariant();
            bag["visibility"] = new Dictionary<string, object?>
            {
                ["mode"] = mode is "any" ? "any" : "all",
                ["conditions"] = extras.Visibility.Conditions
                    .Select(c => new Dictionary<string, object?>
                    {
                        ["fieldId"] = c.FieldId,
                        ["operator"] = string.IsNullOrWhiteSpace(c.Operator) ? "equals" : c.Operator,
                        ["value"] = c.Value
                    })
                    .ToList()
            };
        }

        if (bag.Count == 0 && string.IsNullOrWhiteSpace(legacyRaw))
            return null;

        if (bag.Count == 0)
            return legacyRaw;

        return JsonSerializer.Serialize(bag);
    }

    public static FormFieldFormExtras ParseExtras(string? settingsJson)
    {
        var result = new FormFieldFormExtras();
        if (string.IsNullOrWhiteSpace(settingsJson))
            return result;

        try
        {
            using var doc = JsonDocument.Parse(settingsJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                result.CaptchaExpected = settingsJson.Trim();
                return result;
            }

            if (root.TryGetProperty("defaultValue", out var dv))
                result.DefaultValue = dv.GetString();
            if (root.TryGetProperty("expected", out var exp))
                result.CaptchaExpected = exp.GetString();
            else if (root.TryGetProperty("answer", out var ans))
                result.CaptchaExpected = ans.GetString();

            if (root.TryGetProperty("layout", out var layout) && layout.ValueKind == JsonValueKind.Object
                && layout.TryGetProperty("width", out var width))
                result.LayoutWidth = width.GetString();

            if (root.TryGetProperty("validation", out var validation) && validation.ValueKind == JsonValueKind.Object)
            {
                result.Validation = new FormFieldValidationSchema
                {
                    MinLength = TryInt(validation, "minLength"),
                    MaxLength = TryInt(validation, "maxLength"),
                    Min = TryDecimal(validation, "min"),
                    Max = TryDecimal(validation, "max"),
                    Pattern = TryString(validation, "pattern"),
                    MaxFileSize = TryLong(validation, "maxFileSize"),
                    MinSelections = TryInt(validation, "minSelections"),
                    MaxSelections = TryInt(validation, "maxSelections"),
                    AllowedExtensions = TryStringArray(validation, "allowedExtensions")
                };
                result.MinLength = result.Validation.MinLength;
                result.MaxLength = result.Validation.MaxLength;
                result.Min = result.Validation.Min;
                result.Max = result.Validation.Max;
                result.Pattern = result.Validation.Pattern;
                result.MinSelections = result.Validation.MinSelections;
                result.MaxSelections = result.Validation.MaxSelections;
                if (result.Validation.MaxFileSize is long bytes)
                    result.MaxFileSizeMb = Math.Round(bytes / (1024d * 1024d), 2);
                if (result.Validation.AllowedExtensions is { Count: > 0 } exts)
                    result.AllowedExtensions = string.Join('|', exts);
            }

            if (root.TryGetProperty("visibility", out var vis) && vis.ValueKind == JsonValueKind.Object)
            {
                result.Visibility = JsonSerializer.Deserialize<FormFieldVisibilitySchema>(vis.GetRawText());
            }

            var raw = new Dictionary<string, string>();
            if (root.TryGetProperty("raw", out var rawEl))
                raw["raw"] = rawEl.GetString() ?? "";
            result.RawSettings = raw.Count == 0 ? null : raw;
            result.Layout = string.IsNullOrWhiteSpace(result.LayoutWidth)
                ? null
                : new FormFieldLayoutSchema { Width = result.LayoutWidth! };
        }
        catch (JsonException)
        {
            result.CaptchaExpected = settingsJson.Trim();
        }

        return result;
    }

    private static int? TryInt(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.TryGetInt32(out var v) ? v : null;

    private static long? TryLong(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.TryGetInt64(out var v) ? v : null;

    private static decimal? TryDecimal(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.TryGetDecimal(out var v) ? v : null;

    private static string? TryString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) ? p.GetString() : null;

    private static IReadOnlyList<string>? TryStringArray(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.Array)
            return null;
        return p.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList();
    }
}

public sealed class FormFieldFormExtras
{
    public string? DefaultValue { get; set; }
    public string? LayoutWidth { get; set; } = "full";
    public string? Pattern { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public double? MaxFileSizeMb { get; set; }
    public string? AllowedExtensions { get; set; }
    public int? MinSelections { get; set; }
    public int? MaxSelections { get; set; }
    public string? CaptchaExpected { get; set; }
    public FormFieldValidationSchema? Validation { get; set; }
    public FormFieldVisibilitySchema? Visibility { get; set; }
    public FormFieldLayoutSchema? Layout { get; set; }
    public IReadOnlyDictionary<string, string>? RawSettings { get; set; }

    public bool HasValidation =>
        MinLength is not null || MaxLength is not null || Min is not null || Max is not null
        || !string.IsNullOrWhiteSpace(Pattern) || MaxFileSizeMb is not null
        || !string.IsNullOrWhiteSpace(AllowedExtensions)
        || MinSelections is not null || MaxSelections is not null;
}

using CMS.Modules.Forms.Application.Fields;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Fields;

namespace CMS.Modules.Forms.Application.Submissions;

/// <summary>Resolves stored submission values to Persian (or configured) option labels for display.</summary>
public static class SubmissionValueDisplay
{
    public static bool IsChoiceType(string? fieldTypeId) =>
        fieldTypeId is FormFieldTypeIds.Select
            or FormFieldTypeIds.Radio
            or FormFieldTypeIds.CheckboxGroup;

    public static string? Resolve(
        string? value,
        string? displayValue,
        string? fieldTypeId,
        IReadOnlyList<FormFieldOptionSchema>? options)
    {
        if (!string.IsNullOrWhiteSpace(displayValue))
            return displayValue;

        if (!IsChoiceType(fieldTypeId) || options is not { Count: > 0 })
            return value;

        var pairs = options
            .Select(o => new FormFieldOptionParser.OptionPair(o.Value, o.Label))
            .ToList();
        return FormFieldOptionParser.ToDisplayLabels(value, pairs);
    }

    public static string? Resolve(
        SubmissionDataField field,
        IReadOnlyDictionary<string, FormSchemaField>? schemaByKey)
    {
        FormSchemaField? schemaField = null;
        if (schemaByKey is not null)
            schemaByKey.TryGetValue(field.Key, out schemaField);

        return Resolve(
            field.Value,
            field.DisplayValue,
            field.Type ?? schemaField?.Type,
            schemaField?.Options);
    }
}

using CMS.Modules.Forms.Application.Actions;
using CMS.Modules.Forms.Application.AntiSpam;
using CMS.Modules.Forms.Application.Fields;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Fields;

namespace CMS.Modules.Forms.Application.Public;

/// <summary>
/// Builds a theme-safe <see cref="FormPublicContract"/> from form identity + schema.
/// </summary>
public static class FormPublicContractMapper
{
    private static readonly FormFieldTypeRegistry Registry = new();

    public static FormPublicContract From(
        FormDefinition form,
        FormSchemaDocument? schema,
        Guid? versionId = null,
        FormAntiSpamSchema? antiSpamOverride = null)
    {
        schema ??= FormSchemaLegacyMapper.ToDocument(form);

        var antiSpam = antiSpamOverride ?? schema.AntiSpam ?? new FormAntiSpamSchema();
        var provider = string.IsNullOrWhiteSpace(antiSpam.Provider)
            ? FormAntiSpamProviderIds.Honeypot
            : antiSpam.Provider.Trim().ToLowerInvariant();

        var siteKey = FormActionConfigReader.GetString(antiSpam.Config, "siteKey");

        var submit = schema.SubmitBehavior ?? new FormSubmitBehaviorSchema();
        var submitType = string.IsNullOrWhiteSpace(submit.Type) ? "message" : submit.Type.Trim().ToLowerInvariant();
        var message = submit.Message;
        var url = submit.Url;

        var button = schema.Settings?.SubmitButtonText;
        if (string.IsNullOrWhiteSpace(button))
            button = "ارسال";

        var fields = MapFields(form, schema);

        return new FormPublicContract(
            schema.SchemaVersion <= 0 ? FormSchemaDocument.CurrentSchemaVersion : schema.SchemaVersion,
            form.Id,
            form.Key,
            form.Slug,
            form.Name,
            form.Description,
            versionId ?? form.PublishedVersionId,
            SubmitUrl: $"/forms/{form.Slug}",
            new FormPublicSubmitBehavior(submitType, message, url, submit.PageId),
            new FormPublicSettings(button!),
            new FormPublicAntiSpam(antiSpam.Enabled, provider, siteKey),
            fields);
    }

    private static IReadOnlyList<FormPublicField> MapFields(FormDefinition form, FormSchemaDocument schema)
    {
        if (schema.Fields is { Count: > 0 })
        {
            var schemaFields = schema.Fields
                .OrderBy(f => f.Position)
                .ThenBy(f => f.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return schemaFields.Select(f => MapSchemaField(f, schemaFields)).ToList();
        }

        return form.Fields
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.CreatedAtUtc)
            .Select(MapLegacyField)
            .ToList();
    }

    private static FormPublicField MapSchemaField(FormSchemaField field, IReadOnlyList<FormSchemaField> allFields)
    {
        var width = field.Layout?.Width;
        if (string.IsNullOrWhiteSpace(width))
            width = "full";

        var options = field.Options is { Count: > 0 }
            ? field.Options.Select(o => new FormPublicOption(
                string.IsNullOrWhiteSpace(o.Value) ? o.Label : o.Value,
                string.IsNullOrWhiteSpace(o.Label) ? o.Value : o.Label)).ToList()
            : (IReadOnlyList<FormPublicOption>)[];

        FormPublicFieldValidation? validation = null;
        var v = field.Validation;
        if (v is not null &&
            (v.MinLength is not null || v.MaxLength is not null || v.Min is not null || v.Max is not null
             || !string.IsNullOrWhiteSpace(v.Pattern) || v.AllowedExtensions is { Count: > 0 }
             || v.MaxFileSize is not null || v.MinSelections is not null || v.MaxSelections is not null))
        {
            validation = new FormPublicFieldValidation(
                v.MinLength,
                v.MaxLength,
                v.Min,
                v.Max,
                v.Pattern,
                v.AllowedExtensions,
                v.MaxFileSize,
                v.MinSelections,
                v.MaxSelections);
        }

        var required = field.Required || v?.Required == true;
        var visibility = MapVisibility(field.Visibility, allFields);

        return new FormPublicField(
            field.Id,
            field.Key,
            string.IsNullOrWhiteSpace(field.Type) ? FormFieldTypeIds.Text : field.Type.Trim().ToLowerInvariant(),
            field.Label,
            field.Placeholder,
            field.HelpText,
            required,
            field.DefaultValue,
            width.Trim().ToLowerInvariant(),
            field.Position,
            options,
            validation,
            visibility);
    }

    private static FormPublicField MapLegacyField(FormField field)
    {
        var typeId = Registry.ToTypeId(field.FieldType);
        var options = FormFieldOptionParser.Parse(field.OptionsCsv)
            .Select(o => new FormPublicOption(o.Value, o.Label))
            .ToList();

        var extras = FormFieldSchemaFactory.ParseExtras(field.SettingsJson);
        var width = string.IsNullOrWhiteSpace(extras.LayoutWidth) ? "full" : extras.LayoutWidth.Trim().ToLowerInvariant();
        var visibility = MapVisibility(extras.Visibility, []);

        return new FormPublicField(
            field.Id.ToString("D"),
            field.Key,
            typeId,
            field.Label,
            field.Placeholder,
            field.HelpText,
            field.IsRequired,
            extras.DefaultValue,
            width,
            field.SortOrder,
            options,
            Visibility: visibility);
    }

    private static FormPublicVisibility? MapVisibility(
        FormFieldVisibilitySchema? visibility,
        IReadOnlyList<FormSchemaField> allFields)
    {
        if (visibility?.Conditions is not { Count: > 0 })
            return null;

        var mode = string.IsNullOrWhiteSpace(visibility.Mode)
            ? "all"
            : visibility.Mode.Trim().ToLowerInvariant();
        if (mode is not ("all" or "any"))
            mode = "all";

        var conditions = visibility.Conditions
            .Select(c =>
            {
                var key = ResolveConditionFieldKey(c.FieldId, allFields);
                var op = string.IsNullOrWhiteSpace(c.Operator) ? "equals" : c.Operator.Trim().ToLowerInvariant();
                return new FormPublicVisibilityCondition(key, op, c.Value);
            })
            .ToList();

        return new FormPublicVisibility(mode, conditions);
    }

    private static string ResolveConditionFieldKey(string fieldId, IReadOnlyList<FormSchemaField> allFields)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
            return string.Empty;

        var match = allFields.FirstOrDefault(f =>
            string.Equals(f.Id, fieldId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(f.Key, fieldId, StringComparison.OrdinalIgnoreCase));

        return match?.Key ?? fieldId;
    }
}

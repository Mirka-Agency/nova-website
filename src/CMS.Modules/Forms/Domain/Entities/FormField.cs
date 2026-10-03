using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Forms.Domain.Enums;
using CMS.Modules.Forms.Domain.Fields;

namespace CMS.Modules.Forms.Domain.Entities;

public class FormField : BaseEntity
{
    private FormField()
    {
    }

    public Guid FormId { get; private set; }
    public FormDefinition Form { get; private set; } = null!;
    public string Key { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string? Placeholder { get; private set; }
    public string? HelpText { get; private set; }
    public FormFieldType FieldType { get; private set; }
    public bool IsRequired { get; private set; }
    public string? OptionsCsv { get; private set; }
    public string? SettingsJson { get; private set; }
    public int SortOrder { get; private set; }

    /// <summary>Seeded essential field; Key/Type/Required/Options are immutable in admin.</summary>
    public bool IsSystem { get; private set; }

    public static FormField Create(
        Guid formId,
        string key,
        string label,
        FormFieldType fieldType,
        bool isRequired,
        string? optionsCsv,
        int sortOrder,
        string? placeholder = null,
        string? helpText = null,
        string? settingsJson = null,
        bool isSystem = false)
    {
        Validate(key, label, fieldType, optionsCsv);
        return new FormField
        {
            FormId = formId,
            Key = key.Trim().ToLowerInvariant(),
            Label = label.Trim(),
            Placeholder = Truncate(placeholder, 300),
            HelpText = Truncate(helpText, 1000),
            FieldType = fieldType,
            IsRequired = isRequired,
            OptionsCsv = NormalizeOptions(optionsCsv),
            SettingsJson = Truncate(settingsJson, 4000),
            SortOrder = sortOrder,
            IsSystem = isSystem
        };
    }

    internal void BindToForm(FormDefinition form)
    {
        Form = form ?? throw new DomainException("فرم الزامی است.");
        FormId = form.Id;
    }

    public void Update(
        string key,
        string label,
        FormFieldType fieldType,
        bool isRequired,
        string? optionsCsv,
        int sortOrder,
        string? placeholder = null,
        string? helpText = null,
        string? settingsJson = null)
    {
        Validate(key, label, fieldType, optionsCsv);
        Key = key.Trim().ToLowerInvariant();
        Label = label.Trim();
        Placeholder = Truncate(placeholder, 300);
        HelpText = Truncate(helpText, 1000);
        FieldType = fieldType;
        IsRequired = isRequired;
        OptionsCsv = NormalizeOptions(optionsCsv);
        SettingsJson = Truncate(settingsJson, 4000);
        SortOrder = sortOrder;
        Touch();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = Math.Max(0, sortOrder);
        Touch();
    }

    public void MarkAsSystem()
    {
        IsSystem = true;
        Touch();
    }

    public IReadOnlyList<string> GetOptions() =>
        FormFieldOptionParser.Parse(OptionsCsv).Select(p => p.Value).ToList();

    public IReadOnlyList<(string Value, string Label)> GetOptionPairs() =>
        FormFieldOptionParser.Parse(OptionsCsv).Select(p => (p.Value, p.Label)).ToList();

    private static string? NormalizeOptions(string? optionsCsv) =>
        FormFieldOptionParser.Normalize(optionsCsv);

    private static void Validate(string key, string label, FormFieldType fieldType, string? optionsCsv)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("کلید فیلد الزامی است.");
        if (key.Trim().Length > 100)
            throw new DomainException("کلید فیلد خیلی طولانی است.");
        if (!key.Trim().All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-'))
            throw new DomainException("کلید فیلد فقط می‌تواند شامل حرف لاتین، عدد، خط زیر یا خط تیره باشد.");
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("برچسب فیلد الزامی است.");
        if (label.Trim().Length > 200)
            throw new DomainException("برچسب فیلد خیلی طولانی است.");
        if (!Enum.IsDefined(fieldType))
            throw new DomainException("نوع فیلد نامعتبر است.");
        if (fieldType is FormFieldType.Select or FormFieldType.Radio or FormFieldType.CheckboxGroup)
        {
            var options = NormalizeOptions(optionsCsv);
            if (string.IsNullOrWhiteSpace(options))
                throw new DomainException("فیلد انتخابی باید حداقل یک گزینه داشته باشد.");
        }
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

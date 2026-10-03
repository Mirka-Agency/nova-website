using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Application.Fields;

public sealed class FormFieldTypeRegistry : IFormFieldTypeRegistry
{
    private readonly Dictionary<string, IFormFieldTypeHandler> _byTypeId;
    private readonly Dictionary<FormFieldType, IFormFieldTypeHandler> _byLegacy;

    public FormFieldTypeRegistry()
    {
        IFormFieldTypeHandler[] handlers =
        [
            new TextFieldHandler(),
            new TextAreaFieldHandler(),
            new EmailFieldHandler(),
            new TelFieldHandler(),
            new NumberFieldHandler(),
            new UrlFieldHandler(),
            new SelectFieldHandler(),
            new RadioFieldHandler(),
            new CheckboxFieldHandler(),
            new CheckboxGroupFieldHandler(),
            new DateFieldHandler(),
            new TimeFieldHandler(),
            new DateTimeFieldHandler(),
            new FileFieldHandler(),
            new HiddenFieldHandler(),
            new ConsentFieldHandler(),
            new LayoutFieldHandler(FormFieldTypeIds.Heading, FormFieldType.Heading),
            new LayoutFieldHandler(FormFieldTypeIds.Paragraph, FormFieldType.Paragraph),
            new LayoutFieldHandler(FormFieldTypeIds.Divider, FormFieldType.Divider),
            new CaptchaFieldHandler()
        ];

        _byTypeId = handlers.ToDictionary(h => h.TypeId, StringComparer.OrdinalIgnoreCase);
        _byLegacy = handlers
            .Where(h => h.LegacyEnum.HasValue)
            .ToDictionary(h => h.LegacyEnum!.Value);
        All = handlers;
        AdminSelectable = handlers
            .Where(h => h.TypeId != FormFieldTypeIds.Captcha)
            .ToList();
    }

    public IReadOnlyList<IFormFieldTypeHandler> All { get; }
    public IReadOnlyList<IFormFieldTypeHandler> AdminSelectable { get; }

    public IFormFieldTypeHandler GetRequired(string typeId) =>
        TryGet(typeId) ?? throw new InvalidOperationException($"Unknown form field type '{typeId}'.");

    public IFormFieldTypeHandler? TryGet(string typeId) =>
        string.IsNullOrWhiteSpace(typeId) ? null : _byTypeId.GetValueOrDefault(typeId.Trim());

    public IFormFieldTypeHandler GetForLegacy(FormFieldType fieldType) =>
        _byLegacy.TryGetValue(fieldType, out var handler)
            ? handler
            : GetRequired(FormFieldTypeIds.Text);

    public string ToTypeId(FormFieldType fieldType) => GetForLegacy(fieldType).TypeId;

    public FormFieldType? ToLegacyEnum(string typeId) => TryGet(typeId)?.LegacyEnum;
}

using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Application.Fields;

public sealed class FormFieldValidationRequest
{
    public required FormSchemaField Field { get; init; }
    public string? RawValue { get; init; }
    public FormFieldFileInput? File { get; init; }
}

public sealed class FormFieldFileInput
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long Length { get; init; }
    public required Stream Content { get; init; }
}

public sealed class FormFieldValidationResult
{
    public bool IsValid => Error is null;
    public string? Error { get; init; }
    public string? NormalizedValue { get; init; }
    public bool SkipPersist { get; init; }

    public static FormFieldValidationResult Ok(string? normalized = null, bool skipPersist = false) =>
        new() { NormalizedValue = normalized, SkipPersist = skipPersist };

    public static FormFieldValidationResult Fail(string error) =>
        new() { Error = error };
}

public interface IFormFieldTypeHandler
{
    string TypeId { get; }
    FormFieldType? LegacyEnum { get; }
    bool IsInput { get; }
    bool RequiresOptions { get; }
    FormFieldValidationResult Validate(FormFieldValidationRequest request);
}

public interface IFormFieldTypeRegistry
{
    IFormFieldTypeHandler GetRequired(string typeId);
    IFormFieldTypeHandler? TryGet(string typeId);
    IFormFieldTypeHandler GetForLegacy(FormFieldType fieldType);
    IReadOnlyList<IFormFieldTypeHandler> All { get; }
    IReadOnlyList<IFormFieldTypeHandler> AdminSelectable { get; }
    string ToTypeId(FormFieldType fieldType);
    FormFieldType? ToLegacyEnum(string typeId);
}

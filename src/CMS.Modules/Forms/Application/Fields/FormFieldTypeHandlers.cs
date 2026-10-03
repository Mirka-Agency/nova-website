using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using CMS.Application.Storage;
using CMS.Modules.Forms.Application.Common;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Application.Fields;

internal static class FieldValidationHelpers
{
    private static readonly Regex PhonePattern = new(@"^[\d\s+\-()]{7,20}$", RegexOptions.Compiled);
    private static readonly Regex UrlPattern = new(
        @"^https?://[^\s/$.?#].[^\s]*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static FormFieldValidationResult ValidateRequired(FormSchemaField field, string? value, bool treatFalseAsEmpty = false)
    {
        var required = field.Validation.Required ?? field.Required;
        var empty = string.IsNullOrWhiteSpace(value)
                    || (treatFalseAsEmpty && value is "false" or "0");
        if (required && empty)
            return FormFieldValidationResult.Fail("این فیلد الزامی است.");
        return FormFieldValidationResult.Ok(value);
    }

    public static FormFieldValidationResult ApplyTextRules(FormSchemaField field, string? value)
    {
        var requiredCheck = ValidateRequired(field, value);
        if (!requiredCheck.IsValid)
            return requiredCheck;
        if (string.IsNullOrWhiteSpace(value))
            return FormFieldValidationResult.Ok(null);

        var trimmed = value.Trim();
        if (field.Validation.MinLength is int minLen && trimmed.Length < minLen)
            return FormFieldValidationResult.Fail($"حداقل {minLen} نویسه لازم است.");
        if (field.Validation.MaxLength is int maxLen && trimmed.Length > maxLen)
            return FormFieldValidationResult.Fail($"حداکثر {maxLen} نویسه مجاز است.");
        if (!string.IsNullOrWhiteSpace(field.Validation.Pattern))
        {
            try
            {
                if (!Regex.IsMatch(trimmed, field.Validation.Pattern!))
                    return FormFieldValidationResult.Fail("مقدار با الگوی مجاز مطابقت ندارد.");
            }
            catch (ArgumentException)
            {
                return FormFieldValidationResult.Fail("الگوی اعتبارسنجی فیلد نامعتبر است.");
            }
        }

        return FormFieldValidationResult.Ok(trimmed);
    }

    public static bool IsValidEmail(string value)
    {
        try
        {
            _ = new MailAddress(value);
            return value.Contains('@');
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPhone(string value) => PhonePattern.IsMatch(value);
    public static bool IsValidUrl(string value) => UrlPattern.IsMatch(value);

    public static FormFieldValidationResult ValidateNumber(FormSchemaField field, string? value)
    {
        var requiredCheck = ValidateRequired(field, value);
        if (!requiredCheck.IsValid)
            return requiredCheck;
        if (string.IsNullOrWhiteSpace(value))
            return FormFieldValidationResult.Ok(null);

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            return FormFieldValidationResult.Fail("عدد معتبر وارد کنید.");
        if (field.Validation.Min is decimal min && number < min)
            return FormFieldValidationResult.Fail($"حداقل مقدار مجاز {min} است.");
        if (field.Validation.Max is decimal max && number > max)
            return FormFieldValidationResult.Fail($"حداکثر مقدار مجاز {max} است.");

        return FormFieldValidationResult.Ok(number.ToString(CultureInfo.InvariantCulture));
    }

    public static FormFieldValidationResult ValidateChoice(
        FormSchemaField field,
        string? value,
        bool multi)
    {
        if (!multi)
        {
            var requiredCheck = ValidateRequired(field, value);
            if (!requiredCheck.IsValid)
                return requiredCheck;
            if (string.IsNullOrWhiteSpace(value))
                return FormFieldValidationResult.Ok(null);

            var options = field.Options.Select(o => o.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (options.Count > 0 && !options.Contains(value))
                return FormFieldValidationResult.Fail("گزینه معتبر انتخاب کنید.");
            return FormFieldValidationResult.Ok(value.Trim());
        }

        var selected = string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(['|', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

        var required = field.Validation.Required ?? field.Required;
        if (required && selected.Count == 0)
            return FormFieldValidationResult.Fail("این فیلد الزامی است.");

        var allowed = field.Options.Select(o => o.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (allowed.Count > 0 && selected.Any(s => !allowed.Contains(s)))
            return FormFieldValidationResult.Fail("گزینه معتبر انتخاب کنید.");

        if (field.Validation.MinSelections is int minSel && selected.Count < minSel)
            return FormFieldValidationResult.Fail($"حداقل {minSel} گزینه انتخاب کنید.");
        if (field.Validation.MaxSelections is int maxSel && selected.Count > maxSel)
            return FormFieldValidationResult.Fail($"حداکثر {maxSel} گزینه مجاز است.");

        return FormFieldValidationResult.Ok(selected.Count == 0 ? null : string.Join('|', selected));
    }

    public static FormFieldValidationResult ValidateFile(FormSchemaField field, FormFieldFileInput? file)
    {
        var required = field.Validation.Required ?? field.Required;
        if (file is null || file.Length <= 0)
        {
            return required
                ? FormFieldValidationResult.Fail("این فیلد الزامی است.")
                : FormFieldValidationResult.Ok(null, skipPersist: true);
        }

        var maxSize = field.Validation.MaxFileSize ?? FormFileUploadRules.MaxBytes;
        if (file.Length > maxSize)
            return FormFieldValidationResult.Fail("حجم فایل بیش از حد مجاز است.");

        if (field.Validation.AllowedExtensions is { Count: > 0 } exts)
        {
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext)
                || !exts.Any(e => e.TrimStart('.').Equals(ext.TrimStart('.'), StringComparison.OrdinalIgnoreCase)
                                  || e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                return FormFieldValidationResult.Fail("پسوند فایل مجاز نیست.");
        }

        if (!FormFileUploadRules.Validate(file.Content, file.ContentType, file.Length))
            return FormFieldValidationResult.Fail("فایل نامعتبر است. فقط PDF، Word، متن یا تصویر تا ۱۰ مگابایت مجاز است.");

        if (file.Content.CanSeek)
            file.Content.Position = 0;

        return FormFieldValidationResult.Ok(file.FileName);
    }
}

internal abstract class FormFieldTypeHandlerBase : IFormFieldTypeHandler
{
    public abstract string TypeId { get; }
    public abstract FormFieldType? LegacyEnum { get; }
    public virtual bool IsInput => true;
    public virtual bool RequiresOptions => false;
    public abstract FormFieldValidationResult Validate(FormFieldValidationRequest request);
}

internal sealed class TextFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Text;
    public override FormFieldType? LegacyEnum => FormFieldType.Text;
    public override FormFieldValidationResult Validate(FormFieldValidationRequest request) =>
        FieldValidationHelpers.ApplyTextRules(request.Field, request.RawValue);
}

internal sealed class TextAreaFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.TextArea;
    public override FormFieldType? LegacyEnum => FormFieldType.TextArea;
    public override FormFieldValidationResult Validate(FormFieldValidationRequest request) =>
        FieldValidationHelpers.ApplyTextRules(request.Field, request.RawValue);
}

internal sealed class EmailFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Email;
    public override FormFieldType? LegacyEnum => FormFieldType.Email;

    public override FormFieldValidationResult Validate(FormFieldValidationRequest request)
    {
        var result = FieldValidationHelpers.ApplyTextRules(request.Field, request.RawValue);
        if (!result.IsValid || string.IsNullOrWhiteSpace(result.NormalizedValue))
            return result;
        return FieldValidationHelpers.IsValidEmail(result.NormalizedValue)
            ? result
            : FormFieldValidationResult.Fail("ایمیل معتبر وارد کنید.");
    }
}

internal sealed class TelFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Tel;
    public override FormFieldType? LegacyEnum => FormFieldType.Phone;

    public override FormFieldValidationResult Validate(FormFieldValidationRequest request)
    {
        var result = FieldValidationHelpers.ApplyTextRules(request.Field, request.RawValue);
        if (!result.IsValid || string.IsNullOrWhiteSpace(result.NormalizedValue))
            return result;
        return FieldValidationHelpers.IsValidPhone(result.NormalizedValue)
            ? result
            : FormFieldValidationResult.Fail("شماره تلفن معتبر وارد کنید.");
    }
}

internal sealed class NumberFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Number;
    public override FormFieldType? LegacyEnum => FormFieldType.Number;
    public override FormFieldValidationResult Validate(FormFieldValidationRequest request) =>
        FieldValidationHelpers.ValidateNumber(request.Field, request.RawValue);
}

internal sealed class UrlFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Url;
    public override FormFieldType? LegacyEnum => FormFieldType.Url;

    public override FormFieldValidationResult Validate(FormFieldValidationRequest request)
    {
        var result = FieldValidationHelpers.ApplyTextRules(request.Field, request.RawValue);
        if (!result.IsValid || string.IsNullOrWhiteSpace(result.NormalizedValue))
            return result;
        return FieldValidationHelpers.IsValidUrl(result.NormalizedValue)
            ? result
            : FormFieldValidationResult.Fail("آدرس اینترنتی معتبر وارد کنید.");
    }
}

internal sealed class SelectFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Select;
    public override FormFieldType? LegacyEnum => FormFieldType.Select;
    public override bool RequiresOptions => true;
    public override FormFieldValidationResult Validate(FormFieldValidationRequest request) =>
        FieldValidationHelpers.ValidateChoice(request.Field, request.RawValue, multi: false);
}

internal sealed class RadioFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Radio;
    public override FormFieldType? LegacyEnum => FormFieldType.Radio;
    public override bool RequiresOptions => true;
    public override FormFieldValidationResult Validate(FormFieldValidationRequest request) =>
        FieldValidationHelpers.ValidateChoice(request.Field, request.RawValue, multi: false);
}

internal sealed class CheckboxFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Checkbox;
    public override FormFieldType? LegacyEnum => FormFieldType.Checkbox;

    public override FormFieldValidationResult Validate(FormFieldValidationRequest request)
    {
        var raw = request.RawValue;
        var normalized = string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(raw, "on", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase)
            ? "true"
            : "false";

        var required = request.Field.Validation.Required ?? request.Field.Required;
        if (required && normalized == "false")
            return FormFieldValidationResult.Fail("این فیلد الزامی است.");

        return FormFieldValidationResult.Ok(normalized);
    }
}

internal sealed class CheckboxGroupFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.CheckboxGroup;
    public override FormFieldType? LegacyEnum => FormFieldType.CheckboxGroup;
    public override bool RequiresOptions => true;
    public override FormFieldValidationResult Validate(FormFieldValidationRequest request) =>
        FieldValidationHelpers.ValidateChoice(request.Field, request.RawValue, multi: true);
}

internal sealed class DateFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Date;
    public override FormFieldType? LegacyEnum => FormFieldType.Date;

    public override FormFieldValidationResult Validate(FormFieldValidationRequest request)
    {
        var result = FieldValidationHelpers.ValidateRequired(request.Field, request.RawValue);
        if (!result.IsValid)
            return result;
        if (string.IsNullOrWhiteSpace(request.RawValue))
            return FormFieldValidationResult.Ok(null);

        if (!JalaliDateHelper.TryParse(request.RawValue, out var local))
            return FormFieldValidationResult.Fail("تاریخ معتبر وارد کنید.");

        return FormFieldValidationResult.Ok(JalaliDateHelper.Format(local, includeTime: false));
    }
}

internal sealed class TimeFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Time;
    public override FormFieldType? LegacyEnum => FormFieldType.Time;

    public override FormFieldValidationResult Validate(FormFieldValidationRequest request)
    {
        var result = FieldValidationHelpers.ValidateRequired(request.Field, request.RawValue);
        if (!result.IsValid)
            return result;
        if (string.IsNullOrWhiteSpace(request.RawValue))
            return FormFieldValidationResult.Ok(null);

        if (!TimeOnly.TryParse(request.RawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return FormFieldValidationResult.Fail("ساعت معتبر وارد کنید.");
        return FormFieldValidationResult.Ok(request.RawValue.Trim());
    }
}

internal sealed class DateTimeFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.DateTime;
    public override FormFieldType? LegacyEnum => FormFieldType.DateTime;

    public override FormFieldValidationResult Validate(FormFieldValidationRequest request)
    {
        var result = FieldValidationHelpers.ValidateRequired(request.Field, request.RawValue);
        if (!result.IsValid)
            return result;
        if (string.IsNullOrWhiteSpace(request.RawValue))
            return FormFieldValidationResult.Ok(null);

        if (!JalaliDateHelper.TryParse(request.RawValue, out var local))
            return FormFieldValidationResult.Fail("تاریخ و زمان معتبر وارد کنید.");

        return FormFieldValidationResult.Ok(JalaliDateHelper.Format(local, includeTime: true));
    }
}

internal sealed class FileFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.File;
    public override FormFieldType? LegacyEnum => FormFieldType.FileUpload;
    public override FormFieldValidationResult Validate(FormFieldValidationRequest request) =>
        FieldValidationHelpers.ValidateFile(request.Field, request.File);
}

internal sealed class HiddenFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Hidden;
    public override FormFieldType? LegacyEnum => FormFieldType.Hidden;
    public override FormFieldValidationResult Validate(FormFieldValidationRequest request) =>
        FieldValidationHelpers.ApplyTextRules(request.Field, request.RawValue ?? request.Field.DefaultValue);
}

internal sealed class ConsentFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Consent;
    public override FormFieldType? LegacyEnum => FormFieldType.Consent;

    public override FormFieldValidationResult Validate(FormFieldValidationRequest request)
    {
        var normalized = string.Equals(request.RawValue, "true", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(request.RawValue, "on", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(request.RawValue, "1", StringComparison.OrdinalIgnoreCase)
            ? "true"
            : "false";

        var required = request.Field.Validation.Required ?? request.Field.Required;
        if (required && normalized == "false")
            return FormFieldValidationResult.Fail("پذیرش این مورد الزامی است.");

        return FormFieldValidationResult.Ok(normalized);
    }
}

internal sealed class LayoutFieldHandler : FormFieldTypeHandlerBase
{
    private readonly string _typeId;
    private readonly FormFieldType _legacy;

    public LayoutFieldHandler(string typeId, FormFieldType legacy)
    {
        _typeId = typeId;
        _legacy = legacy;
    }

    public override string TypeId => _typeId;
    public override FormFieldType? LegacyEnum => _legacy;
    public override bool IsInput => false;

    public override FormFieldValidationResult Validate(FormFieldValidationRequest request) =>
        FormFieldValidationResult.Ok(null, skipPersist: true);
}

internal sealed class CaptchaFieldHandler : FormFieldTypeHandlerBase
{
    public override string TypeId => FormFieldTypeIds.Captcha;
    public override FormFieldType? LegacyEnum => FormFieldType.Captcha;
    public override bool IsInput => false;

    public override FormFieldValidationResult Validate(FormFieldValidationRequest request) =>
        FormFieldValidationResult.Ok(null, skipPersist: true);
}

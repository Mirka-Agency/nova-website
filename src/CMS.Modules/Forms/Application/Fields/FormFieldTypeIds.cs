namespace CMS.Modules.Forms.Application.Fields;

/// <summary>Stable snake_case type ids used in SchemaJson (theme-independent).</summary>
public static class FormFieldTypeIds
{
    public const string Text = "text";
    public const string TextArea = "textarea";
    public const string Email = "email";
    public const string Tel = "tel";
    public const string Number = "number";
    public const string Url = "url";
    public const string Select = "select";
    public const string Radio = "radio";
    public const string Checkbox = "checkbox";
    public const string CheckboxGroup = "checkbox_group";
    public const string Date = "date";
    public const string Time = "time";
    public const string DateTime = "datetime";
    public const string File = "file";
    public const string Hidden = "hidden";
    public const string Consent = "consent";
    public const string Heading = "heading";
    public const string Paragraph = "paragraph";
    public const string Divider = "divider";
    public const string Captcha = "captcha"; // legacy — prefer antiSpam

    public static bool IsLayout(string typeId) =>
        typeId is Heading or Paragraph or Divider;

    public static bool IsInput(string typeId) =>
        !IsLayout(typeId) && typeId is not Captcha;
}

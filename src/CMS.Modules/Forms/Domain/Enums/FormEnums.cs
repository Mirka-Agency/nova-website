namespace CMS.Modules.Forms.Domain.Enums;

public enum FormStatus
{
    Draft = 0,
    Published = 1,
    Disabled = 2
}

public enum FormVersionState
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public enum FormFieldType
{
    Text = 0,
    Email = 1,
    TextArea = 2,
    Number = 3,
    Select = 4,
    Checkbox = 5,
    Phone = 6,
    FileUpload = 7,
    Radio = 8,
    Date = 9,
    /// <summary>Legacy; prefer form antiSpam. Kept for existing forms.</summary>
    Captcha = 10,
    Url = 11,
    CheckboxGroup = 12,
    Time = 13,
    DateTime = 14,
    Hidden = 15,
    Consent = 16,
    Heading = 17,
    Paragraph = 18,
    Divider = 19
}

public enum FormTemplateKind
{
    Contact = 1,
    Consultation = 2,
    Hiring = 3
}

public enum SubmissionStatus
{
    /// <summary>New submission (formerly Unread).</summary>
    New = 0,
    Read = 1,
    Archived = 2,
    Processed = 3,
    Spam = 4
}

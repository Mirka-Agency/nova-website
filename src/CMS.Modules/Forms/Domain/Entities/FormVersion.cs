using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Domain.Entities;

/// <summary>
/// Immutable snapshot of a form schema at a point in time.
/// Public submissions always validate against a Published version.
/// </summary>
public class FormVersion : BaseEntity
{
    private FormVersion()
    {
    }

    public Guid FormId { get; private set; }
    public FormDefinition Form { get; private set; } = null!;
    public int VersionNumber { get; private set; }
    public FormVersionState State { get; private set; } = FormVersionState.Draft;
    public string SchemaJson { get; private set; } = "{}";

    public static FormVersion Create(
        Guid formId,
        int versionNumber,
        string schemaJson,
        FormVersionState state = FormVersionState.Draft)
    {
        if (versionNumber < 1)
            throw new DomainException("شماره نسخه باید حداقل ۱ باشد.");
        if (string.IsNullOrWhiteSpace(schemaJson))
            throw new DomainException("اسکیمای نسخه الزامی است.");

        return new FormVersion
        {
            FormId = formId,
            VersionNumber = versionNumber,
            State = state,
            SchemaJson = schemaJson.Trim()
        };
    }

    internal void BindToForm(FormDefinition form)
    {
        Form = form ?? throw new DomainException("فرم الزامی است.");
        FormId = form.Id;
    }

    public void ReplaceSchema(string schemaJson)
    {
        if (State == FormVersionState.Archived)
            throw new DomainException("نسخه بایگانی‌شده قابل ویرایش نیست.");
        if (string.IsNullOrWhiteSpace(schemaJson))
            throw new DomainException("اسکیمای نسخه الزامی است.");

        SchemaJson = schemaJson.Trim();
        Touch();
    }

    public void SetState(FormVersionState state)
    {
        if (!Enum.IsDefined(state))
            throw new DomainException("وضعیت نسخه نامعتبر است.");
        State = state;
        Touch();
    }

    public void MarkPublished() => SetState(FormVersionState.Published);
    public void MarkDraft() => SetState(FormVersionState.Draft);
    public void Archive() => SetState(FormVersionState.Archived);
}

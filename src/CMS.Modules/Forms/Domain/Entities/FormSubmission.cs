using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Domain.Entities;

public class FormSubmission : BaseEntity
{
    private readonly List<FormSubmissionFile> _files = [];

    private FormSubmission()
    {
    }

    public Guid FormId { get; private set; }
    public FormDefinition Form { get; private set; } = null!;
    public Guid FormVersionId { get; private set; }
    public FormVersion FormVersion { get; private set; } = null!;
    public DateTime SubmittedAtUtc { get; private set; }
    public SubmissionStatus Status { get; private set; } = SubmissionStatus.New;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    /// <summary>Canonical field values JSON — the only store for submission field data.</summary>
    public string DataJson { get; private set; } = """{"fields":[]}""";

    /// <summary>Request context JSON (page, locale, utm, referrer).</summary>
    public string ContextJson { get; private set; } = "{}";

    public IReadOnlyCollection<FormSubmissionFile> Files => _files;

    public static FormSubmission Create(
        Guid formId,
        Guid formVersionId,
        string? ipAddress,
        string? userAgent,
        string? dataJson = null,
        string? contextJson = null)
    {
        if (formVersionId == Guid.Empty)
            throw new DomainException("نسخه فرم الزامی است.");

        return new FormSubmission
        {
            FormId = formId,
            FormVersionId = formVersionId,
            SubmittedAtUtc = DateTime.UtcNow,
            Status = SubmissionStatus.New,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim()[..Math.Min(ipAddress.Trim().Length, 64)],
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim()[..Math.Min(userAgent.Trim().Length, 512)],
            DataJson = string.IsNullOrWhiteSpace(dataJson) ? """{"fields":[]}""" : dataJson.Trim(),
            ContextJson = string.IsNullOrWhiteSpace(contextJson) ? "{}" : contextJson.Trim()
        };
    }

    public void SetDataJson(string dataJson)
    {
        if (string.IsNullOrWhiteSpace(dataJson))
            throw new DomainException("داده ارسال الزامی است.");
        DataJson = dataJson.Trim();
        Touch();
    }

    public void SetContextJson(string contextJson)
    {
        ContextJson = string.IsNullOrWhiteSpace(contextJson) ? "{}" : contextJson.Trim();
        Touch();
    }

    public FormSubmissionFile AddFile(
        Guid? fieldId,
        string fieldKey,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        string? publicUrl)
    {
        var file = FormSubmissionFile.Create(
            Id, fieldId, fieldKey, originalFileName, contentType, sizeBytes, storageKey, publicUrl);
        file.BindToSubmission(this);
        _files.Add(file);
        Touch();
        return file;
    }

    public void SetStatus(SubmissionStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new DomainException("وضعیت ارسال نامعتبر است.");
        Status = status;
        Touch();
    }

    public void MarkRead()
    {
        if (Status == SubmissionStatus.Archived || Status == SubmissionStatus.Spam)
            return;
        Status = SubmissionStatus.Read;
        Touch();
    }

    public void MarkUnread() => SetStatus(SubmissionStatus.New);

    public void MarkProcessed() => SetStatus(SubmissionStatus.Processed);

    public void MarkSpam() => SetStatus(SubmissionStatus.Spam);

    public void Archive() => SetStatus(SubmissionStatus.Archived);

    public void Unarchive()
    {
        if (Status != SubmissionStatus.Archived)
            return;
        Status = SubmissionStatus.Read;
        Touch();
    }
}

using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Forms.Domain.Entities;

public class FormSubmissionFile : BaseEntity
{
    private FormSubmissionFile()
    {
    }

    public Guid SubmissionId { get; private set; }
    public FormSubmission Submission { get; private set; } = null!;
    public Guid? FieldId { get; private set; }
    public string FieldKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string? PublicUrl { get; private set; }

    public static FormSubmissionFile Create(
        Guid submissionId,
        Guid? fieldId,
        string fieldKey,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        string? publicUrl)
    {
        if (string.IsNullOrWhiteSpace(fieldKey))
            throw new DomainException("کلید فیلد فایل الزامی است.");
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new DomainException("مسیر ذخیره‌سازی فایل الزامی است.");
        if (sizeBytes <= 0)
            throw new DomainException("حجم فایل نامعتبر است.");

        var safeName = Path.GetFileName(originalFileName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "file";
        if (safeName.Length > 255)
            safeName = safeName[..255];

        return new FormSubmissionFile
        {
            SubmissionId = submissionId,
            FieldId = fieldId,
            FieldKey = fieldKey.Trim().ToLowerInvariant(),
            OriginalFileName = safeName,
            ContentType = string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType.Trim()[..Math.Min(contentType.Trim().Length, 200)],
            SizeBytes = sizeBytes,
            StorageKey = storageKey.Trim(),
            PublicUrl = string.IsNullOrWhiteSpace(publicUrl) ? null : publicUrl.Trim()[..Math.Min(publicUrl.Trim().Length, 2000)]
        };
    }

    internal void BindToSubmission(FormSubmission submission)
    {
        Submission = submission ?? throw new DomainException("ارسال الزامی است.");
        SubmissionId = submission.Id;
    }
}

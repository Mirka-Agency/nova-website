using CMS.Application.Common.Paging;
using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Application.Submissions;

public sealed class SubmissionListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
    public string? Search { get; init; }
    public Guid? FormId { get; init; }
    public SubmissionStatus? Status { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize =>
        PageSize < 1 ? PagedRequest.DefaultPageSize : Math.Min(PageSize, PagedRequest.MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

public sealed record SubmissionListItemDto(
    Guid Id,
    Guid FormId,
    string FormName,
    DateTime SubmittedAtUtc,
    SubmissionStatus Status,
    string? Preview,
    string? IpAddress);

public sealed record SubmissionValueDto(
    Guid? FieldId,
    string FieldKey,
    string? FieldLabel,
    string? Value,
    string? FieldType = null);

public sealed record SubmissionFileDto(
    Guid Id,
    Guid? FieldId,
    string FieldKey,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string? PublicUrl);

public sealed record SubmissionDetailDto(
    Guid Id,
    Guid FormId,
    Guid FormVersionId,
    string FormName,
    DateTime SubmittedAtUtc,
    SubmissionStatus Status,
    string? IpAddress,
    string? UserAgent,
    SubmissionContextDocument? Context,
    IReadOnlyList<SubmissionValueDto> Values,
    IReadOnlyList<SubmissionFileDto> Files);

public sealed record SubmittedFile(
    string FieldKey,
    string FileName,
    string ContentType,
    long Length,
    Stream Content);

public sealed record SubmitFormCommand(
    string Slug,
    IReadOnlyDictionary<string, string?> Values,
    IReadOnlyList<SubmittedFile> Files,
    string? Honeypot,
    string? CaptchaAnswer,
    string? IpAddress,
    string? UserAgent,
    SubmissionContextDocument? Context = null,
    string? AntiSpamToken = null);

public sealed record SubmitResultDto(
    Guid SubmissionId,
    string? SuccessMessage,
    string? RedirectUrl);

public sealed record ExportSubmissionsRequest(
    IReadOnlyList<Guid>? Ids,
    Guid? FormId,
    SubmissionStatus? Status,
    string? Search,
    DateTime? FromUtc,
    DateTime? ToUtc);

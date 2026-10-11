using CMS.Modules.Comments.Domain.Enums;

namespace CMS.Modules.Comments.Application.Import;

public sealed record WordPressCommentXmlItem(
    string ExternalId,
    string? ExternalParentId,
    string AuthorName,
    string? AuthorEmail,
    string Body,
    DateTime PublishedAtUtc,
    CommentStatus Status,
    string PageUrl,
    int LineHint);

public sealed record CommentTargetCandidate(
    CommentTargetType TargetType,
    Guid TargetId,
    string Title,
    string CanonicalPath);

public enum CommentUrlMatchKind
{
    None = 0,
    Exact = 1,
    Ambiguous = 2
}

public sealed record CommentUrlMatchResult(
    CommentUrlMatchKind Kind,
    CommentTargetCandidate? Target,
    IReadOnlyList<CommentTargetCandidate> Candidates);

public enum CommentImportRowDisposition
{
    Ready = 0,
    Duplicate = 1,
    Unmatched = 2,
    Ambiguous = 3,
    Invalid = 4
}

public sealed record CommentImportPreviewRow(
    string ExternalId,
    string? ExternalParentId,
    string AuthorName,
    string? AuthorEmail,
    string Body,
    DateTime PublishedAtUtc,
    CommentStatus Status,
    string PageUrl,
    string NormalizedPath,
    CommentImportRowDisposition Disposition,
    CommentTargetType? MatchedTargetType,
    Guid? MatchedTargetId,
    string? MatchedTargetTitle,
    string? ErrorMessage);

public sealed record CommentImportPreview(
    string PreviewId,
    DateTime CreatedAtUtc,
    int TotalCount,
    int ReadyCount,
    int DuplicateCount,
    int UnmatchedCount,
    int AmbiguousCount,
    int InvalidCount,
    IReadOnlyList<CommentImportPreviewRow> Rows);

public sealed record CommentImportManualOverride(
    string ExternalId,
    CommentTargetType TargetType,
    Guid TargetId,
    string TargetTitle);

public sealed record CommentImportConfirmCommand(
    string PreviewId,
    bool OnlyNew = true,
    IReadOnlyList<CommentImportManualOverride>? ManualOverrides = null);

public sealed record CommentImportRowResult(
    string ExternalId,
    string PageUrl,
    string Outcome,
    string? Message,
    Guid? CommentId,
    string? TargetTitle);

public sealed record CommentImportResult(
    string PreviewId,
    int TotalCount,
    int ImportedCount,
    int DuplicateCount,
    int UnmatchedCount,
    int FailedCount,
    bool Queued,
    IReadOnlyList<CommentImportRowResult> Rows);

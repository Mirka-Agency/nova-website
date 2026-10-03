using CMS.Application.Common.Paging;
using CMS.Modules.Comments.Domain.Enums;

namespace CMS.Modules.Comments.Application.Comments;

public sealed record CommentListItemDto(
    Guid Id,
    CommentTargetType TargetType,
    Guid TargetId,
    string TargetTitle,
    string AuthorName,
    string? AuthorEmail,
    string? AuthorPhone,
    string Body,
    CommentStatus Status,
    DateTime PublishedAtUtc,
    DateTime CreatedAtUtc);

public sealed record PublicCommentDto(
    Guid Id,
    string AuthorName,
    string Body,
    DateTime PublishedAtUtc);

public sealed record SubmitCommentCommand(
    CommentTargetType TargetType,
    Guid TargetId,
    string TargetTitle,
    string? UserId,
    string AuthorName,
    string? AuthorEmail,
    string? AuthorPhone,
    string Body,
    string? CaptchaToken,
    string? RemoteIp);

public sealed record UpdateCommentCommand(
    string Body,
    DateTime PublishedAtUtc);

public sealed record CommentListQuery(
    CommentTargetType? TargetType = null,
    CommentStatus? Status = null,
    string? Search = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = PagedRequest.DefaultPageSize)
{
    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize =>
        PageSize < 1 ? PagedRequest.DefaultPageSize : Math.Min(PageSize, PagedRequest.MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

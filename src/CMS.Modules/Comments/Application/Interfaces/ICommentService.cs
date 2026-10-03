using CMS.Application.Common.Paging;
using CMS.Modules.Comments.Application.Comments;
using CMS.Modules.Comments.Domain.Enums;

namespace CMS.Modules.Comments.Application.Interfaces;

public interface ICommentService
{
    Task<IReadOnlyList<CommentListItemDto>> ListAsync(
        CommentListQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedResult<CommentListItemDto>> ListPagedAsync(
        CommentListQuery query,
        CancellationToken cancellationToken = default);

    Task<CommentListItemDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicCommentDto>> ListApprovedForTargetAsync(
        CommentTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);

    Task<Guid> SubmitAsync(SubmitCommentCommand command, CancellationToken cancellationToken = default);

    Task ApproveAsync(Guid id, CancellationToken cancellationToken = default);

    Task RejectAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, UpdateCommentCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

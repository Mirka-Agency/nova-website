using CMS.Modules.Comments.Domain.Enums;

namespace CMS.Modules.Comments.Application.Import;

public interface ICommentTargetCatalog
{
    Task<IReadOnlyList<CommentTargetCandidate>> ListAsync(CancellationToken cancellationToken = default);

    Task<CommentTargetCandidate?> GetAsync(
        CommentTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);
}

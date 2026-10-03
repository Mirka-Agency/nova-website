using CMS.Application.Common.Paging;
using CMS.Modules.Blog.Application.Posts;

namespace CMS.Modules.Blog.Application.Interfaces;

public interface IPostService
{
    Task<IReadOnlyList<PostListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<PostListItemDto>> ListPagedAsync(PostListRequest request, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<PostDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> FindUserDraftIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Guid> GetOrCreateUserDraftAsync(string userId, CancellationToken cancellationToken = default);
    Task<Guid> DiscardUserDraftAsync(string userId, Guid draftId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SavePostCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SavePostCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

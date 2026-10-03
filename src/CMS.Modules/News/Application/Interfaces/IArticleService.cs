using CMS.Application.Common.Paging;
using CMS.Modules.News.Application.Articles;

namespace CMS.Modules.News.Application.Interfaces;

public interface IArticleService
{
    Task<IReadOnlyList<ArticleListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ArticleListItemDto>> ListPagedAsync(ArticleListRequest request, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<ArticleDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> FindUserDraftIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Guid> GetOrCreateUserDraftAsync(string userId, CancellationToken cancellationToken = default);
    Task<Guid> DiscardUserDraftAsync(string userId, Guid draftId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveArticleCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveArticleCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

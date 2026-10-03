using CMS.Application.Common.Paging;
using CMS.Modules.Video.Application.VideoItems;

namespace CMS.Modules.Video.Application.Interfaces;

public interface IVideoItemService
{
    Task<IReadOnlyList<VideoItemListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<VideoItemListItemDto>> ListPagedAsync(VideoItemListRequest request, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<VideoItemDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> FindUserDraftIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Guid> GetOrCreateUserDraftAsync(string userId, CancellationToken cancellationToken = default);
    Task<Guid> DiscardUserDraftAsync(string userId, Guid draftId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveVideoItemCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveVideoItemCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

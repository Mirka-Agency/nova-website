using CMS.Application.Common.Paging;
using CMS.Modules.Services.Application.ServiceItems;

namespace CMS.Modules.Services.Application.Interfaces;

public interface IServiceItemService
{
    Task<IReadOnlyList<ServiceItemListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ServiceItemListItemDto>> ListPagedAsync(ServiceItemListRequest request, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<ServiceItemDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> FindUserDraftIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Guid> GetOrCreateUserDraftAsync(string userId, CancellationToken cancellationToken = default);
    Task<Guid> DiscardUserDraftAsync(string userId, Guid draftId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveServiceItemCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveServiceItemCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

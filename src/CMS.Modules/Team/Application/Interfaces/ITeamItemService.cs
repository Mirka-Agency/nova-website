using CMS.Application.Common.Paging;
using CMS.Modules.Team.Application.TeamItems;

namespace CMS.Modules.Team.Application.Interfaces;

public interface ITeamItemService
{
    Task<IReadOnlyList<TeamItemListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<TeamItemListItemDto>> ListPagedAsync(TeamItemListRequest request, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<TeamItemDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> FindUserDraftIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Guid> GetOrCreateUserDraftAsync(string userId, CancellationToken cancellationToken = default);
    Task<Guid> DiscardUserDraftAsync(string userId, Guid draftId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveTeamItemCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveTeamItemCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

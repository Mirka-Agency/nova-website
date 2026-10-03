using CMS.Application.Common.Paging;
using CMS.Modules.Team.Application.TeamItems;

namespace CMS.Modules.Team.Application.Interfaces;

public interface IPublicTeamItemQuery
{
    Task<IReadOnlyList<PublicTeamItemSummaryDto>> ListPublishedAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<PublicTeamItemSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<PublicTeamItemDetailDto?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

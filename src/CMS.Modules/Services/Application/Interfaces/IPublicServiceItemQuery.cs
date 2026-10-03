using CMS.Application.Common.Paging;
using CMS.Modules.Services.Application.ServiceItems;

namespace CMS.Modules.Services.Application.Interfaces;

public interface IPublicServiceItemQuery
{
    Task<IReadOnlyList<PublicServiceItemSummaryDto>> ListPublishedAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<PublicServiceItemSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<PublicServiceItemDetailDto?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

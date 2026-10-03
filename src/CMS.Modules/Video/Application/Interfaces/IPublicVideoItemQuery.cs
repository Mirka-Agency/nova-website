using CMS.Application.Common.Paging;
using CMS.Modules.Video.Application.VideoItems;

namespace CMS.Modules.Video.Application.Interfaces;

public interface IPublicVideoItemQuery
{
    Task<IReadOnlyList<PublicVideoItemSummaryDto>> ListPublishedAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<PublicVideoItemSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<PublicVideoItemDetailDto?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

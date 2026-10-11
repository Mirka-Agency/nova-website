using CMS.Application.Common.Paging;
using CMS.Modules.Blog.Application.Posts;

namespace CMS.Modules.Blog.Application.Interfaces;

public interface IPublicPostQuery
{
    /// <summary>Archive uses client-side paging; allow a large single fetch.</summary>
    const int MaxArchivePageSize = 500;

    Task<IReadOnlyList<PublicPostSummaryDto>> ListPublishedAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<PublicPostSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<PublicPostDetailDto?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

using CMS.Application.Common.Paging;
using CMS.Modules.Blog.Application.Posts;

namespace CMS.Modules.Blog.Application.Interfaces;

public interface IPublicPostQuery
{
    /// <summary>Public archive page size (server-side <c>?page=</c> pagination).</summary>
    const int DefaultArchivePageSize = 12;

    /// <summary>Hard cap for a single list fetch (related posts, bulk lookups).</summary>
    const int MaxArchivePageSize = 500;

    Task<IReadOnlyList<PublicPostSummaryDto>> ListPublishedAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<PublicPostSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<PublicPostDetailDto?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicPostSummaryDto>> ListPublishedByIdsPreserveOrderAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>Latest published posts in the same category, excluding <paramref name="excludeId"/>.</summary>
    Task<IReadOnlyList<PublicPostSummaryDto>> ListPublishedRelatedByCategoryAsync(
        Guid categoryId,
        Guid excludeId,
        int take,
        CancellationToken cancellationToken = default);
}

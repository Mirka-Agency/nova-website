using CMS.Application.Common.Paging;
using CMS.Modules.Blog.Application.Posts;

namespace CMS.Modules.Blog.Application.Interfaces;

public interface IPublicPostQuery
{
    /// <summary>Public archive page size for page 2+ (3-column grid).</summary>
    const int DefaultArchivePageSize = 12;

    /// <summary>
    /// Page 1 size: one full-width featured card + 12 grid cards (fills the 3-column grid
    /// so the last row is not left with an empty slot).
    /// </summary>
    const int FirstArchivePageSize = 13;

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

    /// <summary>
    /// Published posts that list <paramref name="postId"/> in their RelatedPostsJson
    /// (reverse related links), newest first.
    /// </summary>
    Task<IReadOnlyList<Guid>> ListPublishedIdsReferencingAsync(
        Guid postId,
        int take,
        CancellationToken cancellationToken = default);
}

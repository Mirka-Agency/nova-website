using CMS.Application.Common.Paging;
using CMS.Modules.News.Application.Articles;
using CMS.Modules.News.Domain.Enums;

namespace CMS.Modules.News.Application.Interfaces;

public interface IPublicArticleQuery
{
    Task<IReadOnlyList<PublicArticleSummaryDto>> ListPublishedAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<PublicArticleSummaryDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<PagedResult<PublicArticleSummaryDto>> ListPublishedByKindPagedAsync(
        ArticleKind kind,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<PublicArticleDetailDto?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

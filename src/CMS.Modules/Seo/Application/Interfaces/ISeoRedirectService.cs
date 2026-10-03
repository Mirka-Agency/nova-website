using CMS.Application.Common.Paging;
using CMS.Modules.Seo.Application.Redirects;

namespace CMS.Modules.Seo.Application.Interfaces;

public interface ISeoRedirectService
{
    Task<PagedResult<SeoRedirectListItemDto>> ListPagedAsync(SeoRedirectListRequest request, CancellationToken cancellationToken = default);
    Task<SeoRedirectDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveSeoRedirectCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveSeoRedirectCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task<SeoRedirectMatchDto?> ResolveAsync(string path, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

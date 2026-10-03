using CMS.Application.Common.Paging;
using CMS.Modules.Honors.Application.HonorItems;

namespace CMS.Modules.Honors.Application.Interfaces;

public interface IHonorItemService
{
    Task<PagedResult<HonorItemListItemDto>> ListPagedAsync(
        HonorItemListRequest request,
        CancellationToken cancellationToken = default);

    Task<HonorItemDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(SaveHonorItemCommand command, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, SaveHonorItemCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task ReorderAsync(IReadOnlyList<Guid> orderedIds, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

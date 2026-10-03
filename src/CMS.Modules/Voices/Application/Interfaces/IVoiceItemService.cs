using CMS.Application.Common.Paging;
using CMS.Modules.Voices.Application.VoiceItems;

namespace CMS.Modules.Voices.Application.Interfaces;

public interface IVoiceItemService
{
    Task<PagedResult<VoiceItemListItemDto>> ListPagedAsync(
        VoiceItemListRequest request,
        CancellationToken cancellationToken = default);

    Task<VoiceItemDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(SaveVoiceItemCommand command, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, SaveVoiceItemCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task ReorderAsync(IReadOnlyList<Guid> orderedIds, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

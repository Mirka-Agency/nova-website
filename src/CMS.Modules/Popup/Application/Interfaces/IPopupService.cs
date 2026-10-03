using CMS.Application.Common.Paging;
using CMS.Modules.Popup.Application.Popups;

namespace CMS.Modules.Popup.Application.Interfaces;

public interface IPopupService
{
    Task<PagedResult<PopupListItemDto>> ListPagedAsync(PopupListRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PopupOptionDto>> ListOptionsAsync(Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<PopupDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SavePopupCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SavePopupCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Flips IsActive and returns the new value.</summary>
    Task<bool> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

public interface IPublicPopupQuery
{
    /// <summary>Returns active popups that match the current request path (server-side page targeting).</summary>
    Task<IReadOnlyList<PublicPopupDto>> GetForPathAsync(string path, CancellationToken cancellationToken = default);
}

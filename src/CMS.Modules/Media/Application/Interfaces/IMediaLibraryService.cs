using CMS.Application.Common.Paging;
using CMS.Modules.Media.Application.Assets;

namespace CMS.Modules.Media.Application.Interfaces;

public interface IMediaLibraryService
{
    Task<IReadOnlyList<MediaAssetListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<MediaAssetListItemDto>> ListPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<MediaAssetDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MediaAssetDetailDto?> FindByPublicUrlAsync(string publicUrl, CancellationToken cancellationToken = default);
    Task<MediaAssetDetailDto> UploadAsync(UploadMediaCommand command, CancellationToken cancellationToken = default);
    Task UpdateMetadataAsync(Guid id, UpdateMediaMetadataCommand command, CancellationToken cancellationToken = default);
    Task<MediaAssetDetailDto> ReplaceFileAsync(Guid id, ReplaceMediaFileCommand command, CancellationToken cancellationToken = default);
    Task<MediaAssetDetailDto> RestoreOriginalAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

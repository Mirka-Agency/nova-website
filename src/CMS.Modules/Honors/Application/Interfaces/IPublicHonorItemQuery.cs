using CMS.Modules.Honors.Application.HonorItems;

namespace CMS.Modules.Honors.Application.Interfaces;

public interface IPublicHonorItemQuery
{
    Task<IReadOnlyList<PublicHonorItemDto>> ListPublishedAsync(CancellationToken cancellationToken = default);
}

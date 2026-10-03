using CMS.Modules.Honors.Application.HonorItems;
using CMS.Modules.Honors.Application.Interfaces;
using CMS.Modules.Honors.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Honors.Infrastructure.Services;

public sealed class PublicHonorItemQuery : IPublicHonorItemQuery
{
    private readonly HonorsDbContext _db;

    public PublicHonorItemQuery(HonorsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PublicHonorItemDto>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.HonorItems.AsNoTracking()
            .Where(h => h.IsPublished)
            .OrderBy(h => h.SortOrder)
            .ThenBy(h => h.CreatedAtUtc)
            .Select(h => new PublicHonorItemDto(
                h.Id,
                h.Title,
                h.ImageUrl,
                h.AltText,
                h.SortOrder))
            .ToListAsync(cancellationToken);
    }
}

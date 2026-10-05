using CMS.Application.Caching;
using CMS.Modules.Honors.Application.HonorItems;
using CMS.Modules.Honors.Application.Interfaces;
using CMS.Modules.Honors.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CMS.Modules.Honors.Infrastructure.Services;

public sealed class PublicHonorItemQuery : IPublicHonorItemQuery
{
    private const string CacheKey = "cms:public:honors:list";
    private readonly HonorsDbContext _db;
    private readonly IMemoryCache _cache;

    public PublicHonorItemQuery(HonorsDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<PublicHonorItemDto>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        return PublicContentCache.GetOrCreateAsync(
            _cache,
            CacheKey,
            async ct => (IReadOnlyList<PublicHonorItemDto>)await _db.HonorItems.AsNoTracking()
                .Where(h => h.IsPublished)
                .OrderBy(h => h.SortOrder)
                .ThenBy(h => h.CreatedAtUtc)
                .Select(h => new PublicHonorItemDto(
                    h.Id,
                    h.Title,
                    h.ImageUrl,
                    h.AltText,
                    h.SortOrder))
                .ToListAsync(ct),
            cancellationToken);
    }
}

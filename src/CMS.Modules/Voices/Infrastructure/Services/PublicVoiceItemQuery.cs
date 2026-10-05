using CMS.Application.Caching;
using CMS.Modules.Voices.Application.Interfaces;
using CMS.Modules.Voices.Application.VoiceItems;
using CMS.Modules.Voices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CMS.Modules.Voices.Infrastructure.Services;

public sealed class PublicVoiceItemQuery : IPublicVoiceItemQuery
{
    private const string CacheKey = "cms:public:voices:list";
    private readonly VoicesDbContext _db;
    private readonly IMemoryCache _cache;

    public PublicVoiceItemQuery(VoicesDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<PublicVoiceItemDto>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        return PublicContentCache.GetOrCreateAsync(
            _cache,
            CacheKey,
            async ct => (IReadOnlyList<PublicVoiceItemDto>)await _db.VoiceItems.AsNoTracking()
                .Where(v => v.IsPublished)
                .OrderBy(v => v.SortOrder)
                .ThenBy(v => v.CreatedAtUtc)
                .Select(v => new PublicVoiceItemDto(
                    v.Id,
                    v.CustomerName,
                    v.Subtitle,
                    v.Description,
                    v.AudioUrl,
                    v.SortOrder))
                .ToListAsync(ct),
            cancellationToken);
    }
}

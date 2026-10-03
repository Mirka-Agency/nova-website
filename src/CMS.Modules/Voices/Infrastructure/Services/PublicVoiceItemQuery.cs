using CMS.Modules.Voices.Application.Interfaces;
using CMS.Modules.Voices.Application.VoiceItems;
using CMS.Modules.Voices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Voices.Infrastructure.Services;

public sealed class PublicVoiceItemQuery : IPublicVoiceItemQuery
{
    private readonly VoicesDbContext _db;

    public PublicVoiceItemQuery(VoicesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PublicVoiceItemDto>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.VoiceItems.AsNoTracking()
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
            .ToListAsync(cancellationToken);
    }
}

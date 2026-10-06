using CMS.Application.Common.Features;
using CMS.Application.Seo;
using CMS.Modules.Video.Domain.Enums;
using CMS.Modules.Video.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Video.Infrastructure.Seo;

public sealed class VideosSitemapUrlProvider : ISitemapUrlProvider
{
    private readonly VideoDbContext _db;

    public VideosSitemapUrlProvider(VideoDbContext db)
    {
        _db = db;
    }

    public string Segment => "videos";

    public string? RequiredFeature => FeatureNames.Video;

    public async Task<IReadOnlyList<SitemapUrlEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.VideoItems
            .AsNoTracking()
            .Where(p => p.Status == VideoStatus.Published)
            .Select(p => new
            {
                p.Slug,
                LastMod = p.UpdatedAtUtc ?? p.PublishedAtUtc ?? p.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var listingLastMod = items.Count > 0
            ? items.Max(p => p.LastMod)
            : DateTime.UtcNow.Date;

        var entries = new List<SitemapUrlEntry>
        {
            new("/Videos", listingLastMod, SitemapChangeFrequency.Daily, 0.8)
        };

        foreach (var item in items)
        {
            entries.Add(new SitemapUrlEntry(
                $"/Videos/{item.Slug}",
                item.LastMod,
                SitemapChangeFrequency.Weekly,
                0.7));
        }

        return entries;
    }
}

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
        var entries = new List<SitemapUrlEntry>
        {
            new("/Videos", DateTime.UtcNow.Date, SitemapChangeFrequency.Daily, 0.8)
        };

        var items = await _db.VideoItems
            .AsNoTracking()
            .Where(p => p.Status == VideoStatus.Published)
            .Select(p => new
            {
                p.Slug,
                LastMod = p.PublishedAtUtc ?? p.UpdatedAtUtc ?? p.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

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

using CMS.Application.Common.Features;
using CMS.Application.Seo;
using CMS.Modules.Services.Domain.Enums;
using CMS.Modules.Services.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Services.Infrastructure.Seo;

public sealed class ServicesSitemapUrlProvider : ISitemapUrlProvider
{
    private readonly ServicesDbContext _db;

    public ServicesSitemapUrlProvider(ServicesDbContext db)
    {
        _db = db;
    }

    public string Segment => "services";

    public string? RequiredFeature => FeatureNames.Services;

    public async Task<IReadOnlyList<SitemapUrlEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.ServiceItems
            .AsNoTracking()
            .Where(p => p.Status == ServiceStatus.Published)
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
            new("/Services", listingLastMod, SitemapChangeFrequency.Daily, 0.8)
        };

        foreach (var item in items)
        {
            entries.Add(new SitemapUrlEntry(
                $"/Services/{item.Slug}",
                item.LastMod,
                SitemapChangeFrequency.Weekly,
                0.7));
        }

        return entries;
    }
}

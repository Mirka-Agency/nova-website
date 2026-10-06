using CMS.Application.Common.Features;
using CMS.Application.Seo;
using CMS.Modules.Team.Domain.Enums;
using CMS.Modules.Team.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Team.Infrastructure.Seo;

public sealed class TeamsSitemapUrlProvider : ISitemapUrlProvider
{
    private readonly TeamDbContext _db;

    public TeamsSitemapUrlProvider(TeamDbContext db)
    {
        _db = db;
    }

    public string Segment => "teams";

    public string? RequiredFeature => FeatureNames.Team;

    public async Task<IReadOnlyList<SitemapUrlEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.TeamItems
            .AsNoTracking()
            .Where(p => p.Status == TeamStatus.Published)
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
            new("/doctors", listingLastMod, SitemapChangeFrequency.Daily, 0.8)
        };

        foreach (var item in items)
        {
            entries.Add(new SitemapUrlEntry(
                $"/doctors/{item.Slug}",
                item.LastMod,
                SitemapChangeFrequency.Weekly,
                0.7));
        }

        return entries;
    }
}

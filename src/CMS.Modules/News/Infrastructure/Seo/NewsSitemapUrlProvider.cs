using CMS.Application.Common.Features;
using CMS.Application.Seo;
using CMS.Modules.News.Domain.Enums;
using CMS.Modules.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.News.Infrastructure.Seo;

public sealed class NewsSitemapUrlProvider : ISitemapUrlProvider
{
    private readonly NewsDbContext _db;

    public NewsSitemapUrlProvider(NewsDbContext db)
    {
        _db = db;
    }

    public string Segment => "news";

    public string? RequiredFeature => FeatureNames.News;

    public async Task<IReadOnlyList<SitemapUrlEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<SitemapUrlEntry>
        {
            new("/news", DateTime.UtcNow.Date, SitemapChangeFrequency.Daily, 0.8),
            new("/events", DateTime.UtcNow.Date, SitemapChangeFrequency.Daily, 0.8)
        };

        var articles = await _db.Articles
            .AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published)
            .Select(a => new
            {
                a.Slug,
                a.Kind,
                LastMod = a.PublishedAtUtc ?? a.UpdatedAtUtc ?? a.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        foreach (var article in articles)
        {
            var path = article.Kind == ArticleKind.Event
                ? $"/events/{article.Slug}"
                : $"/news/{article.Slug}";
            entries.Add(new SitemapUrlEntry(
                path,
                article.LastMod,
                SitemapChangeFrequency.Weekly,
                0.7));
        }

        return entries;
    }
}

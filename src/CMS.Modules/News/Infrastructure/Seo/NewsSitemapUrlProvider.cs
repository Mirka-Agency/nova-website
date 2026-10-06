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
        var articles = await _db.Articles
            .AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published)
            .Select(a => new
            {
                a.Slug,
                a.Kind,
                LastMod = a.UpdatedAtUtc ?? a.PublishedAtUtc ?? a.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var educationArticles = articles.Where(a => a.Kind != ArticleKind.Event).ToList();
        var events = articles.Where(a => a.Kind == ArticleKind.Event).ToList();
        var today = DateTime.UtcNow.Date;

        var entries = new List<SitemapUrlEntry>
        {
            new(
                "/education-articles",
                educationArticles.Count > 0 ? educationArticles.Max(a => a.LastMod) : today,
                SitemapChangeFrequency.Daily,
                0.8),
            new(
                "/event",
                events.Count > 0 ? events.Max(a => a.LastMod) : today,
                SitemapChangeFrequency.Daily,
                0.8)
        };

        foreach (var article in articles)
        {
            var path = article.Kind == ArticleKind.Event
                ? $"/event/{article.Slug}"
                : $"/education-articles/{article.Slug}";
            entries.Add(new SitemapUrlEntry(
                path,
                article.LastMod,
                SitemapChangeFrequency.Weekly,
                0.7));
        }

        return entries;
    }
}

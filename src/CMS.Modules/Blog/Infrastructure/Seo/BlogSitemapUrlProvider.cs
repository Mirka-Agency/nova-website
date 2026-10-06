using CMS.Application.Common.Features;
using CMS.Application.Seo;
using CMS.Modules.Blog.Domain.Enums;
using CMS.Modules.Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Blog.Infrastructure.Seo;

public sealed class BlogSitemapUrlProvider : ISitemapUrlProvider
{
    private readonly BlogDbContext _db;

    public BlogSitemapUrlProvider(BlogDbContext db)
    {
        _db = db;
    }

    public string Segment => "blog";

    public string? RequiredFeature => FeatureNames.Blog;

    public async Task<IReadOnlyList<SitemapUrlEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var posts = await _db.Posts
            .AsNoTracking()
            .Where(p => p.Status == PostStatus.Published)
            .Select(p => new
            {
                p.Slug,
                LastMod = p.UpdatedAtUtc ?? p.PublishedAtUtc ?? p.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var listingLastMod = posts.Count > 0
            ? posts.Max(p => p.LastMod)
            : DateTime.UtcNow.Date;

        var entries = new List<SitemapUrlEntry>
        {
            new("/blog", listingLastMod, SitemapChangeFrequency.Daily, 0.8)
        };

        foreach (var post in posts)
        {
            entries.Add(new SitemapUrlEntry(
                $"/blog/{post.Slug}",
                post.LastMod,
                SitemapChangeFrequency.Weekly,
                0.7));
        }

        return entries;
    }
}

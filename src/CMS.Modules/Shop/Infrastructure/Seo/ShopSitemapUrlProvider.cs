using CMS.Application.Common.Features;
using CMS.Application.Seo;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Shop.Infrastructure.Seo;

public sealed class ShopSitemapUrlProvider : ISitemapUrlProvider
{
    private readonly ShopDbContext _db;

    public ShopSitemapUrlProvider(ShopDbContext db)
    {
        _db = db;
    }

    public string Segment => "shop";

    public string? RequiredFeature => FeatureNames.Shop;

    public async Task<IReadOnlyList<SitemapUrlEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new
            {
                c.Slug,
                LastMod = c.UpdatedAtUtc ?? c.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active)
            .Select(p => new
            {
                p.Slug,
                LastMod = p.UpdatedAtUtc ?? p.PublishedAtUtc ?? p.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var listingCandidates = categories.Select(c => c.LastMod)
            .Concat(products.Select(p => p.LastMod))
            .ToList();
        var listingLastMod = listingCandidates.Count > 0
            ? listingCandidates.Max()
            : DateTime.UtcNow.Date;

        var entries = new List<SitemapUrlEntry>
        {
            new("/shop", listingLastMod, SitemapChangeFrequency.Daily, 0.8)
        };

        foreach (var category in categories)
        {
            entries.Add(new SitemapUrlEntry(
                $"/shop/category/{category.Slug}",
                category.LastMod,
                SitemapChangeFrequency.Weekly,
                0.6));
        }

        foreach (var product in products)
        {
            entries.Add(new SitemapUrlEntry(
                $"/shop/{product.Slug}",
                product.LastMod,
                SitemapChangeFrequency.Weekly,
                0.7));
        }

        return entries;
    }
}

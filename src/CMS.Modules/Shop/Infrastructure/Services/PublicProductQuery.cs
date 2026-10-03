using CMS.Application.Common.Paging;
using CMS.Modules.Shop.Application.Categories;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Pricing;
using CMS.Modules.Shop.Application.Products;
using CMS.Modules.Shop.Application.Settings;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class PublicProductQuery : IPublicProductQuery
{
    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 48;

    private readonly ShopDbContext _db;
    private readonly IPricingEngine _pricing;
    private readonly IShopSettingsService _settings;

    public PublicProductQuery(ShopDbContext db, IPricingEngine pricing, IShopSettingsService settings)
    {
        _db = db;
        _pricing = pricing;
        _settings = settings;
    }

    public async Task<IReadOnlyList<PublicProductListItemDto>> ListPublishedAsync(
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var page = await ListPublishedPagedAsync(1, DefaultPageSize, userId, null, cancellationToken);
        return page.Items;
    }

    public async Task<PagedResult<PublicProductListItemDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        string? userId = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1
            ? DefaultPageSize
            : Math.Min(pageSize, MaxPageSize);

        var settings = await _settings.GetAsync(cancellationToken);
        var customerGroupId = await _pricing.ResolveCustomerGroupIdAsync(userId, cancellationToken);
        var showPrice = userId is not null || settings.ShowPricesToGuests;

        var query = _db.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                Excerpt = p.ShortDescription != string.Empty
                    ? p.ShortDescription
                    : (p.Description.Length > 160 ? p.Description.Substring(0, 160) : p.Description),
                p.Price,
                p.Currency,
                p.IsAvailable,
                p.IsPurchasable,
                CategoryName = p.Category != null ? p.Category.Name : null,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                p.CoverImageUrl
            })
            .ToListAsync(cancellationToken);

        var productIds = products.Select(p => p.Id).ToList();
        var reviewStats = productIds.Count == 0
            ? new Dictionary<Guid, (int Count, double Average)>()
            : await _db.Reviews
                .AsNoTracking()
                .Where(r => r.IsApproved && productIds.Contains(r.ProductId))
                .GroupBy(r => r.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Count = g.Count(),
                    Average = g.Average(r => (double)r.Rating)
                })
                .ToDictionaryAsync(
                    x => x.ProductId,
                    x => (x.Count, x.Average),
                    cancellationToken);

        IReadOnlyDictionary<(Guid ProductId, Guid? VariationId), PriceQuoteResult> quotes =
            new Dictionary<(Guid ProductId, Guid? VariationId), PriceQuoteResult>();
        if (showPrice && products.Count > 0)
        {
            var requests = products
                .Select(p => new PriceQuoteRequest(p.Id, null, 1, customerGroupId))
                .ToList();
            quotes = await _pricing.QuoteManyAsync(requests, cancellationToken);
        }

        var result = new List<PublicProductListItemDto>(products.Count);
        foreach (var p in products)
        {
            decimal? displayPrice = null;
            if (showPrice && quotes.TryGetValue((p.Id, null), out var quote))
                displayPrice = quote.UnitPrice;

            reviewStats.TryGetValue(p.Id, out var stats);

            result.Add(new PublicProductListItemDto(
                p.Id,
                p.Title,
                p.Slug,
                p.Excerpt,
                p.Price,
                displayPrice,
                p.Currency,
                p.IsAvailable,
                CanPurchase(settings, p.IsAvailable, p.IsPurchasable),
                showPrice,
                p.CategoryName,
                p.BrandName,
                p.CoverImageUrl,
                stats.Count > 0 ? stats.Average : null,
                stats.Count));
        }

        return new PagedResult<PublicProductListItemDto>(result, totalCount, normalizedPage, normalizedPageSize);
    }

    public async Task<PublicCategoryDetailDto?> GetPublishedCategoryBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.Slug == normalized && c.IsActive)
            .Select(c => new PublicCategoryDetailDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.Content,
                c.ImageUrl,
                c.MetaTitle,
                c.MetaDescription,
                c.SeoKeywords))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PublicProductDetailDto?> GetPublishedBySlugAsync(
        string slug,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var settings = await _settings.GetAsync(cancellationToken);
        var customerGroupId = await _pricing.ResolveCustomerGroupIdAsync(userId, cancellationToken);
        var showPrice = userId is not null || settings.ShowPricesToGuests;

        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Variations)
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Slug == normalized && p.Status == ProductStatus.Active, cancellationToken);

        if (product is null)
            return null;

        var approvedReviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == product.Id && r.IsApproved)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new PublicReviewDto(r.Id, r.AuthorName, r.Rating, r.Comment, r.AdminResponse, r.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var activeVariations = product.Variations.Where(v => v.Status == VariationStatus.Active).ToList();
        decimal displayPrice = product.Price;
        var variationPrices = new Dictionary<Guid, decimal>();

        if (showPrice)
        {
            var quoteRequests = new List<PriceQuoteRequest>
            {
                new(product.Id, null, 1, customerGroupId)
            };
            quoteRequests.AddRange(activeVariations.Select(v =>
                new PriceQuoteRequest(product.Id, v.Id, 1, customerGroupId)));

            var quotes = await _pricing.QuoteManyAsync(quoteRequests, cancellationToken);
            if (quotes.TryGetValue((product.Id, null), out var productQuote))
                displayPrice = productQuote.UnitPrice;

            foreach (var v in activeVariations)
            {
                if (quotes.TryGetValue((product.Id, v.Id), out var vQuote))
                    variationPrices[v.Id] = vQuote.UnitPrice;
            }
        }

        var variations = activeVariations.Select(v => new PublicVariationDto(
            v.Id,
            v.Sku,
            v.AttributeSummary,
            v.Price,
            variationPrices.TryGetValue(v.Id, out var priced) ? priced : v.EffectivePrice,
            v.StockQuantity,
            v.UnlimitedStock,
            v.ImageUrl,
            v.Status)).ToList();

        return new PublicProductDetailDto(
            product.Id,
            product.Title,
            product.Slug,
            product.ShortDescription,
            product.Description,
            product.Price,
            displayPrice,
            product.Currency,
            product.IsAvailable,
            CanPurchase(settings, product.IsAvailable, product.IsPurchasable),
            showPrice,
            product.ProductType,
            product.Category?.Name,
            product.Brand?.Name,
            product.CoverImageUrl,
            product.VideoUrl,
            product.MinimumOrderQuantity,
            product.StockQuantity,
            product.UnlimitedStock,
            product.MetaTitle,
            product.MetaDescription,
            product.SeoKeywords,
            product.CanonicalUrl,
            product.OgTitle,
            product.OgDescription,
            product.OgImageUrl,
            product.FaqJson,
            product.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.SortOrder, i.IsMain))
                .ToList(),
            variations,
            approvedReviews,
            approvedReviews.Count > 0 ? approvedReviews.Average(r => r.Rating) : null,
            approvedReviews.Count);
    }

    private static bool CanPurchase(ShopSettingsDto settings, bool isAvailable, bool isPurchasable)
    {
        if (!settings.EcommerceEnabled || !isAvailable)
            return false;

        return settings.Mode switch
        {
            ShopMode.OnlineStore => true,
            ShopMode.Hybrid => isPurchasable,
            _ => false
        };
    }
}

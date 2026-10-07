using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Domain.Entities;

public class Product : BaseEntity
{
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductVariation> _variations = [];
    private readonly List<ProductSpecification> _specifications = [];

    private Product()
    {
    }

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string ShortDescription { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public decimal? SalePrice { get; private set; }
    public DateTime? SaleStartsAtUtc { get; private set; }
    public DateTime? SaleEndsAtUtc { get; private set; }
    public string Currency { get; private set; } = "IRR";
    public bool IsAvailable { get; private set; } = true;
    public bool IsPurchasable { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Draft;
    public ProductType ProductType { get; private set; } = ProductType.Simple;
    public string? CoverImageUrl { get; private set; }
    public string? CoverImageAlt { get; private set; }
    public string? VideoUrl { get; private set; }
    public Guid? CategoryId { get; private set; }
    public ShopCategory? Category { get; private set; }
    public Guid? BrandId { get; private set; }
    public ProductBrand? Brand { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public int? StockQuantity { get; private set; }
    public bool UnlimitedStock { get; private set; }
    public int LowStockThreshold { get; private set; } = 5;
    public decimal? Weight { get; private set; }
    public int MinimumOrderQuantity { get; private set; } = 1;
    /// <summary>Optional per-product wholesale minimum quantity (overrides group/settings when set).</summary>
    public int? WholesaleMinimumOrderQuantity { get; private set; }
    /// <summary>Optional per-product wholesale minimum line amount.</summary>
    public decimal? WholesaleMinimumOrderAmount { get; private set; }
    public string? Sku { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? SeoKeywords { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public string? OgTitle { get; private set; }
    public string? OgDescription { get; private set; }
    public string? OgImageUrl { get; private set; }
    public string? FaqJson { get; private set; }

    public IReadOnlyCollection<ProductImage> Images => _images;
    public IReadOnlyCollection<ProductVariation> Variations => _variations;
    public IReadOnlyCollection<ProductSpecification> Specifications => _specifications;

    public static Product Create(
        string title,
        string slug,
        string? shortDescription,
        string? description,
        decimal price,
        decimal? salePrice,
        DateTime? saleStartsAtUtc,
        DateTime? saleEndsAtUtc,
        string? currency,
        bool isAvailable,
        bool isPurchasable,
        Guid? categoryId,
        Guid? brandId,
        string? coverImageUrl,
        string? coverImageAlt,
        string? videoUrl,
        int? stockQuantity,
        bool unlimitedStock,
        int lowStockThreshold,
        decimal? weight,
        int minimumOrderQuantity,
        int? wholesaleMinimumOrderQuantity,
        decimal? wholesaleMinimumOrderAmount,
        string? sku,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? faqJson)
    {
        var product = new Product();
        product.Apply(
            title, slug, shortDescription, description, price, salePrice, saleStartsAtUtc, saleEndsAtUtc,
            currency, isAvailable, isPurchasable, categoryId, brandId, coverImageUrl, coverImageAlt, videoUrl,
            stockQuantity, unlimitedStock, lowStockThreshold, weight, minimumOrderQuantity,
            wholesaleMinimumOrderQuantity, wholesaleMinimumOrderAmount, sku,
            metaTitle, metaDescription, seoKeywords, canonicalUrl, ogTitle, ogDescription, ogImageUrl, faqJson);
        return product;
    }

    public void Update(
        string title,
        string slug,
        string? shortDescription,
        string? description,
        decimal price,
        decimal? salePrice,
        DateTime? saleStartsAtUtc,
        DateTime? saleEndsAtUtc,
        string? currency,
        bool isAvailable,
        bool isPurchasable,
        Guid? categoryId,
        Guid? brandId,
        string? coverImageUrl,
        string? coverImageAlt,
        string? videoUrl,
        int? stockQuantity,
        bool unlimitedStock,
        int lowStockThreshold,
        decimal? weight,
        int minimumOrderQuantity,
        int? wholesaleMinimumOrderQuantity,
        decimal? wholesaleMinimumOrderAmount,
        string? sku,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? faqJson)
    {
        Apply(
            title, slug, shortDescription, description, price, salePrice, saleStartsAtUtc, saleEndsAtUtc,
            currency, isAvailable, isPurchasable, categoryId, brandId, coverImageUrl, coverImageAlt, videoUrl,
            stockQuantity, unlimitedStock, lowStockThreshold, weight, minimumOrderQuantity,
            wholesaleMinimumOrderQuantity, wholesaleMinimumOrderAmount, sku,
            metaTitle, metaDescription, seoKeywords, canonicalUrl, ogTitle, ogDescription, ogImageUrl, faqJson);
        Touch();
    }

    public void SetStatus(ProductStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new DomainException("وضعیت محصول نامعتبر است.");

        Status = status;
        if (status == ProductStatus.Active)
            PublishedAtUtc ??= DateTime.UtcNow;
        Touch();
    }

    public void Publish() => SetStatus(ProductStatus.Active);

    public void Unpublish() => SetStatus(ProductStatus.Draft);

    public void ReplaceImages(IEnumerable<ProductImage> images)
    {
        _images.Clear();
        foreach (var image in images)
            _images.Add(image);
        Touch();
    }

    public void ReplaceSpecifications(IEnumerable<ProductSpecification> specifications)
    {
        _specifications.Clear();
        foreach (var specification in specifications)
            _specifications.Add(specification);
        Touch();
    }

    public ProductVariation AddVariation(ProductVariation variation)
    {
        _variations.Add(variation);
        SyncProductTypeFromVariations();
        Touch();
        return variation;
    }

    public bool RemoveVariation(Guid variationId)
    {
        var removed = _variations.RemoveAll(v => v.Id == variationId) > 0;
        if (removed)
        {
            SyncProductTypeFromVariations();
            Touch();
        }

        return removed;
    }

    public void ClearVariations()
    {
        _variations.Clear();
        SyncProductTypeFromVariations();
        Touch();
    }

    /// <summary>
    /// Simple when there are no variations; Variable when at least one exists.
    /// </summary>
    public void SyncProductTypeFromVariations()
    {
        var next = _variations.Count > 0 ? ProductType.Variable : ProductType.Simple;
        if (ProductType == next)
            return;
        ProductType = next;
    }

    public decimal GetEffectiveRetailPrice(DateTime utcNow)
    {
        if (SalePrice is > 0 and var sale && sale < Price)
        {
            var inWindow = (!SaleStartsAtUtc.HasValue || SaleStartsAtUtc <= utcNow)
                && (!SaleEndsAtUtc.HasValue || SaleEndsAtUtc >= utcNow);
            if (inWindow)
                return sale;
        }

        return Price;
    }

    public StockStatus ResolveStockStatus(int? totalQty)
    {
        if (UnlimitedStock)
            return StockStatus.Available;
        var qty = totalQty ?? StockQuantity ?? 0;
        if (qty <= 0)
            return StockStatus.OutOfStock;
        if (qty <= LowStockThreshold)
            return StockStatus.LowStock;
        return StockStatus.Available;
    }

    public void DecrementStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("تعداد باید مثبت باشد.");
        if (UnlimitedStock)
            return;

        var available = StockQuantity ?? 0;
        if (available < quantity)
            throw new DomainException("موجودی این محصول کافی نیست.");

        StockQuantity = available - quantity;
        Touch();
    }

    public void RestoreStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("تعداد باید مثبت باشد.");
        if (UnlimitedStock)
            return;

        StockQuantity = (StockQuantity ?? 0) + quantity;
        Touch();
    }

    private void Apply(
        string title,
        string slug,
        string? shortDescription,
        string? description,
        decimal price,
        decimal? salePrice,
        DateTime? saleStartsAtUtc,
        DateTime? saleEndsAtUtc,
        string? currency,
        bool isAvailable,
        bool isPurchasable,
        Guid? categoryId,
        Guid? brandId,
        string? coverImageUrl,
        string? coverImageAlt,
        string? videoUrl,
        int? stockQuantity,
        bool unlimitedStock,
        int lowStockThreshold,
        decimal? weight,
        int minimumOrderQuantity,
        int? wholesaleMinimumOrderQuantity,
        decimal? wholesaleMinimumOrderAmount,
        string? sku,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? faqJson)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("عنوان محصول الزامی است.");
        if (title.Trim().Length > 300)
            throw new DomainException("عنوان محصول خیلی طولانی است.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("نامک محصول الزامی است.");
        if (slug.Trim().Length > 300)
            throw new DomainException("نامک محصول خیلی طولانی است.");
        if (price < 0)
            throw new DomainException("قیمت نمی‌تواند منفی باشد.");
        if (salePrice is < 0)
            throw new DomainException("قیمت فروش نمی‌تواند منفی باشد.");
        if (lowStockThreshold < 0)
            throw new DomainException("آستانه موجودی کم نامعتبر است.");
        if (minimumOrderQuantity < 1)
            throw new DomainException("حداقل تعداد سفارش باید حداقل ۱ باشد.");
        if (wholesaleMinimumOrderQuantity is < 1)
            throw new DomainException("حداقل تعداد عمده باید حداقل ۱ باشد.");
        if (wholesaleMinimumOrderAmount is < 0)
            throw new DomainException("حداقل مبلغ عمده نمی‌تواند منفی باشد.");
        if (weight is < 0)
            throw new DomainException("وزن نمی‌تواند منفی باشد.");
        if (coverImageAlt is { Length: > 300 })
            throw new DomainException("متن جایگزین تصویر شاخص خیلی طولانی است.");

        Title = title.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        ShortDescription = shortDescription?.Trim() ?? string.Empty;
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        SalePrice = salePrice;
        SaleStartsAtUtc = saleStartsAtUtc;
        SaleEndsAtUtc = saleEndsAtUtc;
        Currency = string.IsNullOrWhiteSpace(currency) ? "IRR" : currency.Trim().ToUpperInvariant();
        if (Currency.Length > 8)
            throw new DomainException("کد ارز خیلی طولانی است.");
        IsAvailable = isAvailable;
        IsPurchasable = isPurchasable;
        CategoryId = categoryId;
        BrandId = brandId;
        CoverImageUrl = string.IsNullOrWhiteSpace(coverImageUrl) ? null : coverImageUrl.Trim();
        CoverImageAlt = string.IsNullOrWhiteSpace(coverImageAlt) ? null : coverImageAlt.Trim();
        VideoUrl = string.IsNullOrWhiteSpace(videoUrl) ? null : videoUrl.Trim();
        UnlimitedStock = unlimitedStock;
        StockQuantity = unlimitedStock ? null : stockQuantity;
        LowStockThreshold = lowStockThreshold;
        Weight = weight;
        MinimumOrderQuantity = minimumOrderQuantity;
        WholesaleMinimumOrderQuantity = wholesaleMinimumOrderQuantity;
        WholesaleMinimumOrderAmount = wholesaleMinimumOrderAmount;
        Sku = string.IsNullOrWhiteSpace(sku) ? null : sku.Trim().ToUpperInvariant();
        MetaTitle = Truncate(metaTitle, 200);
        MetaDescription = Truncate(metaDescription, 500);
        SeoKeywords = Truncate(seoKeywords, 500);
        CanonicalUrl = Truncate(canonicalUrl, 1000);
        OgTitle = Truncate(ogTitle, 200);
        OgDescription = Truncate(ogDescription, 500);
        OgImageUrl = Truncate(ogImageUrl, 1000);
        FaqJson = string.IsNullOrWhiteSpace(faqJson) ? null : faqJson.Trim();
        SyncProductTypeFromVariations();
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

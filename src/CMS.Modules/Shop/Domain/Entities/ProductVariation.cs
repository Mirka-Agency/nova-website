using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Domain.Entities;

public class ProductVariation : BaseEntity
{
    private ProductVariation()
    {
    }

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public string Sku { get; private set; } = string.Empty;
    public string? AttributeSummary { get; private set; }
    public decimal Price { get; private set; }
    public decimal? SalePrice { get; private set; }
    public int? StockQuantity { get; private set; }
    public bool UnlimitedStock { get; private set; }
    public string? ImageUrl { get; private set; }
    public decimal? Weight { get; private set; }
    public VariationStatus Status { get; private set; } = VariationStatus.Active;

    public static ProductVariation Create(
        Guid productId,
        string sku,
        string? attributeSummary,
        decimal price,
        decimal? salePrice,
        int? stockQuantity,
        bool unlimitedStock,
        string? imageUrl,
        decimal? weight)
    {
        var variation = new ProductVariation { ProductId = productId };
        variation.Apply(sku, attributeSummary, price, salePrice, stockQuantity, unlimitedStock, imageUrl, weight, VariationStatus.Active);
        return variation;
    }

    public void Update(
        string sku,
        string? attributeSummary,
        decimal price,
        decimal? salePrice,
        int? stockQuantity,
        bool unlimitedStock,
        string? imageUrl,
        decimal? weight,
        VariationStatus status)
    {
        Apply(sku, attributeSummary, price, salePrice, stockQuantity, unlimitedStock, imageUrl, weight, status);
        Touch();
    }

    public decimal EffectivePrice => SalePrice is > 0 and var s && s < Price ? s : Price;

    public void DecrementStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("تعداد باید مثبت باشد.");
        if (UnlimitedStock)
            return;

        var available = StockQuantity ?? 0;
        if (available < quantity)
            throw new DomainException("موجودی این تنوع کافی نیست.");

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
        string sku,
        string? attributeSummary,
        decimal price,
        decimal? salePrice,
        int? stockQuantity,
        bool unlimitedStock,
        string? imageUrl,
        decimal? weight,
        VariationStatus status)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("کد SKU الزامی است.");
        if (price < 0)
            throw new DomainException("قیمت نمی‌تواند منفی باشد.");
        if (salePrice is < 0)
            throw new DomainException("قیمت فروش نمی‌تواند منفی باشد.");
        if (weight is < 0)
            throw new DomainException("وزن نمی‌تواند منفی باشد.");
        if (!Enum.IsDefined(status))
            throw new DomainException("وضعیت تنوع نامعتبر است.");

        Sku = sku.Trim().ToUpperInvariant();
        AttributeSummary = string.IsNullOrWhiteSpace(attributeSummary) ? null : attributeSummary.Trim();
        Price = price;
        SalePrice = salePrice;
        UnlimitedStock = unlimitedStock;
        StockQuantity = unlimitedStock ? null : stockQuantity;
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        Weight = weight;
        Status = status;
    }
}

public class ProductVariationAttributeValue
{
    public Guid VariationId { get; set; }
    public ProductVariation Variation { get; set; } = null!;
    public Guid AttributeValueId { get; set; }
    public ProductAttributeValue AttributeValue { get; set; } = null!;
}

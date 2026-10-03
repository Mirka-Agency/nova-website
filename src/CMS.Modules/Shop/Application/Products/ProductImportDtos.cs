namespace CMS.Modules.Shop.Application.Products;

public static class ProductCsvColumns
{
    public const string Title = "Title";
    public const string Slug = "Slug";
    public const string Sku = "Sku";
    public const string Price = "Price";
    public const string SalePrice = "SalePrice";
    public const string Currency = "Currency";
    public const string Status = "Status";
    public const string IsAvailable = "IsAvailable";
    public const string IsPurchasable = "IsPurchasable";
    public const string CategorySlug = "CategorySlug";
    public const string BrandSlug = "BrandSlug";
    public const string ShortDescription = "ShortDescription";
    public const string Description = "Description";
    public const string StockQuantity = "StockQuantity";
    public const string UnlimitedStock = "UnlimitedStock";
    public const string Weight = "Weight";
    public const string CoverImageUrl = "CoverImageUrl";
    public const string VideoUrl = "VideoUrl";
    public const string MetaTitle = "MetaTitle";
    public const string MetaDescription = "MetaDescription";
    public const string SeoKeywords = "SeoKeywords";

    public static readonly IReadOnlyList<string> All =
    [
        Title, Slug, Sku, Price, SalePrice, Currency, Status, IsAvailable, IsPurchasable,
        CategorySlug, BrandSlug, ShortDescription, Description, StockQuantity, UnlimitedStock,
        Weight, CoverImageUrl, VideoUrl, MetaTitle, MetaDescription, SeoKeywords
    ];

    public static readonly IReadOnlyList<string> Required = [Title, Price];
}

public sealed record ProductImportRowError(int RowNumber, string Message);

public sealed record ProductImportResult(
    int TotalRows,
    int CreatedCount,
    int FailedCount,
    IReadOnlyList<ProductImportRowError> Errors);

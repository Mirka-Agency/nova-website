using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Catalog;
using CMS.Modules.Shop.Application.Categories;
using CMS.Modules.Shop.Application.Common;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Products;
using CMS.Modules.Shop.Domain.Enums;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class ProductImportService : IProductImportService
{
    private readonly IProductService _products;
    private readonly IShopCategoryService _categories;
    private readonly IBrandService _brands;
    private readonly IShopSettingsService _shopSettings;

    public ProductImportService(
        IProductService products,
        IShopCategoryService categories,
        IBrandService brands,
        IShopSettingsService shopSettings)
    {
        _products = products;
        _categories = categories;
        _brands = brands;
        _shopSettings = shopSettings;
    }

    public byte[] GetSampleCsv() =>
        CsvHelper.Write(ProductCsvColumns.All, [GetSampleRow()]);

    public async Task<ProductImportResult> ImportAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        CsvHelper.CsvTable table;
        try
        {
            table = CsvHelper.Parse(csvStream);
        }
        catch (InvalidOperationException ex)
        {
            return new ProductImportResult(0, 0, 1, [new ProductImportRowError(0, ex.Message)]);
        }

        if (table.Headers.Count == 0)
            return new ProductImportResult(0, 0, 1, [new ProductImportRowError(0, "فایل CSV خالی است.")]);

        var missingColumns = ProductCsvColumns.Required
            .Where(required => !table.Headers.Any(h => string.Equals(h, required, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missingColumns.Count > 0)
            return new ProductImportResult(
                0,
                0,
                1,
                [new ProductImportRowError(0, $"ستون‌های الزامی یافت نشد: {string.Join("، ", missingColumns)}")]);

        var settings = await _shopSettings.GetAsync(cancellationToken);
        var categoryLookup = BuildSlugLookup(
            (await _categories.ListAsync(cancellationToken)).Select(c => (c.Slug, c.Name, c.Id)));
        var brandLookup = BuildSlugLookup(
            (await _brands.ListAsync(cancellationToken)).Select(b => (b.Slug, b.Name, b.Id)));

        var errors = new List<ProductImportRowError>();
        var created = 0;

        for (var index = 0; index < table.Rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = CsvHelper.ToRowDictionary(table.Headers, table.Rows[index]);

            if (!TryBuildCommand(row, settings.IsCatalogOnly, categoryLookup, brandLookup, out var command, out var error))
            {
                errors.Add(new ProductImportRowError(rowNumber, error!));
                continue;
            }

            try
            {
                await _products.CreateAsync(command, cancellationToken);
                created++;
            }
            catch (DomainValidationException ex)
            {
                errors.Add(new ProductImportRowError(rowNumber, FlattenValidation(ex)));
            }
            catch (NotFoundException ex)
            {
                errors.Add(new ProductImportRowError(rowNumber, ex.Message));
            }
            catch (DomainException ex)
            {
                errors.Add(new ProductImportRowError(rowNumber, ex.Message));
            }
        }

        return new ProductImportResult(table.Rows.Count, created, errors.Count, errors);
    }

    private static bool TryBuildCommand(
        IReadOnlyDictionary<string, string> row,
        bool catalogOnly,
        IReadOnlyDictionary<string, Guid> categoryLookup,
        IReadOnlyDictionary<string, Guid> brandLookup,
        out SaveProductCommand command,
        out string? error)
    {
        command = null!;
        error = null;

        CsvHelper.TryGet(row, ProductCsvColumns.Title, out var title);
        if (string.IsNullOrWhiteSpace(title))
        {
            error = "عنوان الزامی است.";
            return false;
        }

        CsvHelper.TryGet(row, ProductCsvColumns.Price, out var priceText);
        if (!CsvHelper.TryParseDecimal(priceText, out var price, out error))
            return false;

        if (price < 0)
        {
            error = "قیمت نمی‌تواند منفی باشد.";
            return false;
        }

        CsvHelper.TryGet(row, ProductCsvColumns.SalePrice, out var salePriceText);
        if (!CsvHelper.TryParseDecimal(salePriceText, out var salePriceValue, out error))
            return false;

        decimal? salePrice = string.IsNullOrWhiteSpace(salePriceText) ? null : salePriceValue;
        if (salePrice is < 0)
        {
            error = "قیمت فروش نمی‌تواند منفی باشد.";
            return false;
        }

        CsvHelper.TryGet(row, ProductCsvColumns.StockQuantity, out var stockText);
        if (!CsvHelper.TryParseInt(stockText, out var stockQuantity, out error))
            return false;

        CsvHelper.TryGet(row, ProductCsvColumns.UnlimitedStock, out var unlimitedStockText);
        if (!CsvHelper.TryParseBool(unlimitedStockText, defaultValue: false, out var unlimitedStock, out error))
            return false;

        CsvHelper.TryGet(row, ProductCsvColumns.IsAvailable, out var isAvailableText);
        if (!CsvHelper.TryParseBool(isAvailableText, defaultValue: true, out var isAvailable, out error))
            return false;

        CsvHelper.TryGet(row, ProductCsvColumns.IsPurchasable, out var isPurchasableText);
        if (!CsvHelper.TryParseBool(isPurchasableText, defaultValue: false, out var isPurchasable, out error))
            return false;

        if (catalogOnly)
            isPurchasable = false;

        CsvHelper.TryGet(row, ProductCsvColumns.Weight, out var weightText);
        if (!CsvHelper.TryParseDecimal(weightText, out var weightValue, out error))
            return false;

        decimal? weight = string.IsNullOrWhiteSpace(weightText) ? null : weightValue;
        if (weight is < 0)
        {
            error = "وزن نمی‌تواند منفی باشد.";
            return false;
        }

        CsvHelper.TryGet(row, ProductCsvColumns.Currency, out var currency);
        currency = string.IsNullOrWhiteSpace(currency) ? "IRR" : currency.Trim().ToUpperInvariant();

        CsvHelper.TryGet(row, ProductCsvColumns.Status, out var statusText);
        var status = ProductStatus.Draft;
        if (!string.IsNullOrWhiteSpace(statusText)
            && !Enum.TryParse(statusText.Trim(), ignoreCase: true, out status))
        {
            error = $"وضعیت نامعتبر: {statusText}";
            return false;
        }

        CsvHelper.TryGet(row, ProductCsvColumns.Slug, out var slug);
        CsvHelper.TryGet(row, ProductCsvColumns.Sku, out var sku);
        CsvHelper.TryGet(row, ProductCsvColumns.ShortDescription, out var shortDescription);
        CsvHelper.TryGet(row, ProductCsvColumns.Description, out var description);
        CsvHelper.TryGet(row, ProductCsvColumns.CoverImageUrl, out var coverImageUrl);
        CsvHelper.TryGet(row, ProductCsvColumns.VideoUrl, out var videoUrl);
        CsvHelper.TryGet(row, ProductCsvColumns.MetaTitle, out var metaTitle);
        CsvHelper.TryGet(row, ProductCsvColumns.MetaDescription, out var metaDescription);
        CsvHelper.TryGet(row, ProductCsvColumns.SeoKeywords, out var seoKeywords);

        CsvHelper.TryGet(row, ProductCsvColumns.CategorySlug, out var categorySlug);
        Guid? categoryId = null;
        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            if (!TryResolveLookup(categorySlug, categoryLookup, out categoryId))
            {
                error = $"دسته با نامک «{categorySlug}» یافت نشد.";
                return false;
            }
        }

        CsvHelper.TryGet(row, ProductCsvColumns.BrandSlug, out var brandSlug);
        Guid? brandId = null;
        if (!string.IsNullOrWhiteSpace(brandSlug))
        {
            if (!TryResolveLookup(brandSlug, brandLookup, out brandId))
            {
                error = $"برند با نامک «{brandSlug}» یافت نشد.";
                return false;
            }
        }

        command = new SaveProductCommand(
            title,
            slug,
            shortDescription,
            description,
            price,
            salePrice,
            null,
            null,
            currency,
            isAvailable,
            isPurchasable,
            status,
            categoryId,
            brandId,
            coverImageUrl,
            videoUrl,
            unlimitedStock ? null : stockQuantity,
            unlimitedStock,
            5,
            weight,
            1,
            null,
            null,
            sku,
            metaTitle,
            metaDescription,
            seoKeywords,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        return true;
    }

    private static IReadOnlyList<string> GetSampleRow() =>
    [
        "نمونه محصول",
        "sample-product",
        "SAMPLE-001",
        "1500000",
        "1200000",
        "IRR",
        "Active",
        "true",
        "true",
        "electronics",
        "samsung",
        "توضیح کوتاه نمونه",
        "<p>توضیحات کامل محصول نمونه</p>",
        "50",
        "false",
        "0.5",
        "https://example.com/images/sample.jpg",
        "",
        "عنوان سئو نمونه",
        "توضیح متا نمونه",
        "کلمه۱, کلمه۲"
    ];

    private static Dictionary<string, Guid> BuildSlugLookup(IEnumerable<(string Slug, string Name, Guid Id)> items)
    {
        var lookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.Slug))
                lookup.TryAdd(item.Slug.Trim(), item.Id);
            if (!string.IsNullOrWhiteSpace(item.Name))
                lookup.TryAdd(item.Name.Trim(), item.Id);
        }

        return lookup;
    }

    private static bool TryResolveLookup(string value, IReadOnlyDictionary<string, Guid> lookup, out Guid? id)
    {
        if (lookup.TryGetValue(value.Trim(), out var resolved))
        {
            id = resolved;
            return true;
        }

        id = null;
        return false;
    }

    private static string FlattenValidation(DomainValidationException ex) =>
        string.Join(" ", ex.Errors.SelectMany(pair => pair.Value));
}

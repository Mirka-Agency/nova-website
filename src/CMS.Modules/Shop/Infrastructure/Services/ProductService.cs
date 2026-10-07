using CMS.Application.Common.Paging;
using CMS.Application.Security;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Common;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Products;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class ProductService : IProductService
{
    private readonly ShopDbContext _db;
    private readonly IValidator<SaveProductCommand> _validator;
    private readonly IHtmlContentSanitizer _htmlSanitizer;

    public ProductService(
        ShopDbContext db,
        IValidator<SaveProductCommand> validator,
        IHtmlContentSanitizer htmlSanitizer)
    {
        _db = db;
        _validator = validator;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<IReadOnlyList<ProductListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(new ProductListRequest { Page = 1, PageSize = PagedRequest.MaxPageSize }, cancellationToken);
        return page.Items;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.Products.AsNoTracking().CountAsync(cancellationToken);

    public Task<PagedResult<ProductListItemDto>> ListPagedAsync(
        PagedRequest request,
        CancellationToken cancellationToken = default) =>
        ListPagedAsync(
            new ProductListRequest
            {
                Page = request.Page,
                PageSize = request.PageSize,
                Search = request.Search
            },
            cancellationToken);

    public async Task<PagedResult<ProductListItemDto>> ListPagedAsync(
        ProductListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p =>
                p.Title.Contains(term) ||
                p.Slug.Contains(term) ||
                (p.Sku != null && p.Sku.Contains(term)) ||
                (p.Category != null && p.Category.Name.Contains(term)) ||
                (p.Brand != null && p.Brand.Name.Contains(term)));
        }

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        if (request.BrandId.HasValue)
            query = query.Where(p => p.BrandId == request.BrandId.Value);

        if (request.ProductType.HasValue)
            query = query.Where(p => p.ProductType == request.ProductType.Value);

        if (request.IsAvailable.HasValue)
            query = query.Where(p => p.IsAvailable == request.IsAvailable.Value);

        if (request.IsPurchasable.HasValue)
            query = query.Where(p => p.IsPurchasable == request.IsPurchasable.Value);

        query = ApplyStockFilter(query, request.Stock);

        var total = await query.CountAsync(cancellationToken);
        query = ApplySort(query, request.Sort);

        var items = await query
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(p => new ProductListItemDto(
                p.Id,
                p.Title,
                p.Slug,
                p.Price,
                p.Currency,
                p.IsAvailable,
                p.IsPurchasable,
                p.Status,
                p.ProductType,
                p.Category != null ? p.Category.Name : null,
                p.Brand != null ? p.Brand.Name : null,
                p.CoverImageUrl,
                p.Variations.Count,
                p.Specifications.Count,
                p.StockQuantity,
                p.UnlimitedStock,
                p.Variations.Any(v => v.UnlimitedStock),
                p.Variations.Sum(v => v.StockQuantity ?? 0),
                p.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    private static IQueryable<Product> ApplyStockFilter(IQueryable<Product> query, string? stock)
    {
        return (stock?.Trim().ToLowerInvariant()) switch
        {
            "unlimited" => query.Where(p =>
                p.UnlimitedStock || p.Variations.Any(v => v.UnlimitedStock)),
            "in" => query.Where(p =>
                p.UnlimitedStock
                || p.Variations.Any(v => v.UnlimitedStock)
                || (p.Variations.Count == 0 && p.StockQuantity != null && p.StockQuantity > 0)
                || (p.Variations.Count > 0 && p.Variations.Sum(v => v.StockQuantity ?? 0) > 0)),
            "out" => query.Where(p =>
                !p.UnlimitedStock
                && !p.Variations.Any(v => v.UnlimitedStock)
                && ((p.Variations.Count == 0 && (p.StockQuantity == null || p.StockQuantity <= 0))
                    || (p.Variations.Count > 0 && p.Variations.Sum(v => v.StockQuantity ?? 0) <= 0))),
            "low" => query.Where(p =>
                !p.UnlimitedStock
                && p.Variations.Count == 0
                && p.StockQuantity != null
                && p.StockQuantity > 0
                && p.StockQuantity <= p.LowStockThreshold),
            _ => query
        };
    }

    private static IQueryable<Product> ApplySort(IQueryable<Product> query, string? sort)
    {
        return (sort?.Trim().ToLowerInvariant()) switch
        {
            "title" => query.OrderBy(p => p.Title),
            "title_desc" => query.OrderByDescending(p => p.Title),
            "price" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "stock" => query
                .OrderBy(p => p.UnlimitedStock || p.Variations.Any(v => v.UnlimitedStock) ? 1 : 0)
                .ThenBy(p => p.Variations.Count > 0
                    ? p.Variations.Sum(v => v.StockQuantity ?? 0)
                    : (p.StockQuantity ?? 0)),
            "status" => query.OrderBy(p => p.Status).ThenByDescending(p => p.CreatedAtUtc),
            _ => query.OrderByDescending(p => p.CreatedAtUtc)
        };
    }

    public async Task<ProductDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Variations)
            .Include(p => p.Specifications)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product is null ? null : Map(product);
    }

    public async Task<Guid> CreateAsync(SaveProductCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        await EnsureRefsAsync(command, cancellationToken);
        await EnsureUniqueSkuAsync(command.Sku, null, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), null, cancellationToken);
        var sanitized = SanitizeCommand(command);
        var product = BuildProduct(sanitized, slug);
        ApplyChildren(product, sanitized, trackNewChildren: false);
        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    public async Task UpdateAsync(Guid id, SaveProductCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        await EnsureRefsAsync(command, cancellationToken);
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variations)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), id, cancellationToken);
        var sanitized = SanitizeCommand(command);
        await EnsureUniqueSkuAsync(sanitized.Sku, id, cancellationToken);
        product.Update(
            sanitized.Title, slug, sanitized.ShortDescription, sanitized.Description, sanitized.Price, sanitized.SalePrice,
            sanitized.SaleStartsAtUtc, sanitized.SaleEndsAtUtc, sanitized.Currency, sanitized.IsAvailable, sanitized.IsPurchasable,
            sanitized.CategoryId, sanitized.BrandId, sanitized.CoverImageUrl, sanitized.CoverImageAlt, sanitized.VideoUrl,
            sanitized.StockQuantity, sanitized.UnlimitedStock, sanitized.LowStockThreshold, sanitized.Weight,
            sanitized.MinimumOrderQuantity, sanitized.WholesaleMinimumOrderQuantity, sanitized.WholesaleMinimumOrderAmount,
            sanitized.Sku, sanitized.MetaTitle, sanitized.MetaDescription, sanitized.SeoKeywords,
            sanitized.CanonicalUrl, sanitized.OgTitle, sanitized.OgDescription, sanitized.OgImageUrl, sanitized.FaqJson);
        product.SetStatus(sanitized.Status);

        if (sanitized.Images is not null)
            _db.ProductImages.RemoveRange(product.Images);
        if (sanitized.Variations is not null)
            _db.Variations.RemoveRange(product.Variations);
        await _db.SaveChangesAsync(cancellationToken);

        ApplyChildren(product, sanitized, trackNewChildren: true);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);
        _db.Products.Remove(product);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddVariationAsync(
        Guid productId,
        SaveProductVariationCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .Include(p => p.Variations)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), productId);

        var variation = ProductVariation.Create(
            product.Id,
            command.Sku,
            command.AttributeSummary,
            command.Price,
            command.SalePrice,
            command.StockQuantity,
            command.UnlimitedStock,
            command.ImageUrl,
            command.Weight);

        product.AddVariation(variation);
        // Pre-assigned Guid keys can be tracked as Modified unless explicitly added.
        _db.Variations.Add(variation);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateVariationAsync(
        Guid productId,
        Guid variationId,
        SaveProductVariationCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .Include(p => p.Variations)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), productId);

        var variation = product.Variations.FirstOrDefault(v => v.Id == variationId)
            ?? throw new NotFoundException(nameof(ProductVariation), variationId);

        variation.Update(
            command.Sku,
            command.AttributeSummary,
            command.Price,
            command.SalePrice,
            command.StockQuantity,
            command.UnlimitedStock,
            command.ImageUrl,
            command.Weight,
            command.Status);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteVariationAsync(
        Guid productId,
        Guid variationId,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .Include(p => p.Variations)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), productId);

        var variation = product.Variations.FirstOrDefault(v => v.Id == variationId)
            ?? throw new NotFoundException(nameof(ProductVariation), variationId);

        if (!product.RemoveVariation(variationId))
            throw new NotFoundException(nameof(ProductVariation), variationId);

        _db.Variations.Remove(variation);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceSpecificationsAsync(
        Guid productId,
        IReadOnlyList<SaveProductSpecificationCommand> specifications,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .Include(p => p.Specifications)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), productId);

        // Commit the removal of existing specifications before adding the
        // replacements, mirroring UpdateAsync's Images/Variations handling —
        // clearing the domain collection and re-adding within the same
        // SaveChanges batch produced duplicate DELETE statements for the
        // removed rows.
        if (product.Specifications.Count > 0)
            _db.ProductSpecifications.RemoveRange(product.Specifications);
        await _db.SaveChangesAsync(cancellationToken);

        var items = (specifications ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Key) && !string.IsNullOrWhiteSpace(s.Value))
            .Select((s, index) => ProductSpecification.Create(
                product.Id,
                s.Key,
                s.Value,
                s.SortOrder > 0 ? s.SortOrder : index))
            .ToList();

        product.ReplaceSpecifications(items);
        // Pre-assigned Guid keys can be tracked as Modified unless explicitly added.
        _db.ProductSpecifications.AddRange(items);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private SaveProductCommand SanitizeCommand(SaveProductCommand command) =>
        command with
        {
            ShortDescription = _htmlSanitizer.Sanitize(command.ShortDescription),
            Description = _htmlSanitizer.Sanitize(command.Description)
        };

    private static Product BuildProduct(SaveProductCommand command, string slug)
    {
        var product = Product.Create(
            command.Title, slug, command.ShortDescription, command.Description, command.Price, command.SalePrice,
            command.SaleStartsAtUtc, command.SaleEndsAtUtc, command.Currency, command.IsAvailable, command.IsPurchasable,
            command.CategoryId, command.BrandId, command.CoverImageUrl, command.CoverImageAlt, command.VideoUrl,
            command.StockQuantity, command.UnlimitedStock, command.LowStockThreshold, command.Weight,
            command.MinimumOrderQuantity, command.WholesaleMinimumOrderQuantity, command.WholesaleMinimumOrderAmount,
            command.Sku, command.MetaTitle, command.MetaDescription, command.SeoKeywords,
            command.CanonicalUrl, command.OgTitle, command.OgDescription, command.OgImageUrl, command.FaqJson);
        product.SetStatus(command.Status);
        return product;
    }

    private void ApplyChildren(Product product, SaveProductCommand command, bool trackNewChildren)
    {
        if (command.Images is not null)
        {
            var images = command.Images
                .Where(i => !string.IsNullOrWhiteSpace(i.Url))
                .Select(i => ProductImage.Create(product.Id, i.Url, i.AltText, i.SortOrder, i.IsMain))
                .ToList();
            product.ReplaceImages(images);
            // Pre-assigned Guid keys can be tracked as Modified unless explicitly added (update path).
            if (trackNewChildren)
                _db.ProductImages.AddRange(images);
        }

        if (command.Variations is null)
            return;

        product.ClearVariations();
        foreach (var v in command.Variations)
        {
            var variation = ProductVariation.Create(
                product.Id, v.Sku, v.AttributeSummary, v.Price, v.SalePrice,
                v.StockQuantity, v.UnlimitedStock, v.ImageUrl, v.Weight);
            product.AddVariation(variation);
            if (trackNewChildren)
                _db.Variations.Add(variation);
        }
    }

    private async Task EnsureRefsAsync(SaveProductCommand command, CancellationToken cancellationToken)
    {
        if (command.CategoryId.HasValue &&
            !await _db.Categories.AnyAsync(c => c.Id == command.CategoryId.Value, cancellationToken))
            throw new NotFoundException(nameof(ShopCategory), command.CategoryId.Value);

        if (command.BrandId.HasValue &&
            !await _db.Brands.AnyAsync(b => b.Id == command.BrandId.Value, cancellationToken))
            throw new NotFoundException(nameof(ProductBrand), command.BrandId.Value);
    }

    private async Task ValidateAsync(SaveProductCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static string ResolveSlug(SaveProductCommand command)
    {
        var slug = SlugGenerator.FromTitle(command.Slug ?? string.Empty, 300);
        if (string.IsNullOrWhiteSpace(slug))
            slug = SynonymSlug(command.Title);

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                [nameof(command.Slug)] = ["نامک الزامی است."]
            });

        return slug;
    }

    private static string SynonymSlug(string title)
    {
        var fromTitle = SlugGenerator.FromTitle(title);
        return string.IsNullOrWhiteSpace(fromTitle) ? $"product-{Guid.NewGuid():N}"[..16] : fromTitle;
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken)
    {
        var candidate = slug;
        var suffix = 2;
        while (await _db.Products.AnyAsync(
                   p => p.Slug == candidate && (!excludeId.HasValue || p.Id != excludeId.Value),
                   cancellationToken))
        {
            candidate = $"{slug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private async Task EnsureUniqueSkuAsync(string? sku, Guid? excludeProductId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return;

        var normalized = sku.Trim().ToUpperInvariant();

        if (await _db.Products.AnyAsync(
                p => p.Sku == normalized && (!excludeProductId.HasValue || p.Id != excludeProductId.Value),
                cancellationToken))
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                [nameof(SaveProductCommand.Sku)] = ["این کد قبلاً برای محصول دیگری ثبت شده است."]
            });
        }

        if (await _db.Variations.AnyAsync(v => v.Sku == normalized, cancellationToken))
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                [nameof(SaveProductCommand.Sku)] = ["این کد قبلاً برای تنوع دیگری ثبت شده است."]
            });
        }
    }

    private static ProductDetailDto Map(Product p) =>
        new(
            p.Id, p.Title, p.Slug, p.ShortDescription, p.Description, p.Price, p.SalePrice,
            p.SaleStartsAtUtc, p.SaleEndsAtUtc, p.Currency, p.IsAvailable, p.IsPurchasable, p.Status, p.ProductType,
            p.CategoryId, p.BrandId, p.CoverImageUrl, p.CoverImageAlt, p.VideoUrl, p.StockQuantity, p.UnlimitedStock,
            p.LowStockThreshold, p.Weight, p.MinimumOrderQuantity,
            p.WholesaleMinimumOrderQuantity, p.WholesaleMinimumOrderAmount,
            p.Sku, p.MetaTitle, p.MetaDescription,
            p.SeoKeywords, p.CanonicalUrl, p.OgTitle, p.OgDescription, p.OgImageUrl, p.FaqJson,
            p.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.SortOrder, i.IsMain)).ToList(),
            p.Variations.Select(v => new ProductVariationDto(
                v.Id, v.Sku, v.AttributeSummary, v.Price, v.SalePrice, v.StockQuantity, v.UnlimitedStock,
                v.ImageUrl, v.Weight, v.Status)).ToList(),
            p.Specifications.OrderBy(s => s.SortOrder).Select(s => new ProductSpecificationDto(s.Id, s.Key, s.Value, s.SortOrder)).ToList());
}

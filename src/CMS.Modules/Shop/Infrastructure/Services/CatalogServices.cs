using CMS.Application.Security;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Catalog;
using CMS.Modules.Shop.Application.Common;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class BrandService : IBrandService
{
    private readonly ShopDbContext _db;
    private readonly IValidator<SaveBrandCommand> _validator;
    private readonly IHtmlContentSanitizer _htmlSanitizer;

    public BrandService(
        ShopDbContext db,
        IValidator<SaveBrandCommand> validator,
        IHtmlContentSanitizer htmlSanitizer)
    {
        _db = db;
        _validator = validator;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<IReadOnlyList<BrandListItemDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.Brands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BrandListItemDto(b.Id, b.Name, b.Slug, b.IsActive, b.LogoUrl))
            .ToListAsync(cancellationToken);

    public async Task<BrandDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _db.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        return brand is null
            ? null
            : new BrandDetailDto(
                brand.Id,
                brand.Name,
                brand.Slug,
                brand.Description,
                brand.Content,
                brand.LogoUrl,
                brand.MetaTitle,
                brand.MetaDescription,
                brand.SeoKeywords,
                brand.IsActive);
    }

    public async Task<Guid> CreateAsync(SaveBrandCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command.Name, command.Slug), null, cancellationToken);
        var content = SanitizeContent(command.Content);
        var brand = ProductBrand.Create(
            command.Name,
            slug,
            command.Description,
            content,
            command.LogoUrl,
            command.MetaTitle,
            command.MetaDescription,
            command.SeoKeywords);
        if (!command.IsActive)
        {
            brand.Update(
                command.Name,
                slug,
                command.Description,
                content,
                command.LogoUrl,
                command.MetaTitle,
                command.MetaDescription,
                command.SeoKeywords,
                false);
        }

        _db.Brands.Add(brand);
        await _db.SaveChangesAsync(cancellationToken);
        return brand.Id;
    }

    public async Task UpdateAsync(Guid id, SaveBrandCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var brand = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductBrand), id);

        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command.Name, command.Slug), id, cancellationToken);
        var content = SanitizeContent(command.Content);
        brand.Update(
            command.Name,
            slug,
            command.Description,
            content,
            command.LogoUrl,
            command.MetaTitle,
            command.MetaDescription,
            command.SeoKeywords,
            command.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductBrand), id);
        _db.Brands.Remove(brand);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private string? SanitizeContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var sanitized = _htmlSanitizer.Sanitize(content);
        return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
    }

    private async Task ValidateAsync(SaveBrandCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken)
    {
        var candidate = slug;
        var suffix = 2;
        while (await _db.Brands.AnyAsync(b => b.Slug == candidate && (!excludeId.HasValue || b.Id != excludeId.Value), cancellationToken))
        {
            candidate = $"{slug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static string ResolveSlug(string name, string? slug)
    {
        var resolved = SlugGenerator.FromTitle(slug ?? string.Empty);
        if (string.IsNullOrWhiteSpace(resolved))
            resolved = SlugGenerator.FromTitle(name);
        if (string.IsNullOrWhiteSpace(resolved))
            throw new DomainValidationException(new Dictionary<string, string[]> { [nameof(slug)] = ["نامک الزامی است."] });
        return resolved;
    }
}

public sealed class AttributeService : IAttributeService
{
    private readonly ShopDbContext _db;

    public AttributeService(ShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AttributeListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var attrs = await _db.Attributes
            .AsNoTracking()
            .Include(a => a.Values)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return attrs.Select(Map).ToList();
    }

    public async Task<AttributeListItemDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var attr = await _db.Attributes
            .AsNoTracking()
            .Include(a => a.Values)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return attr is null ? null : Map(attr);
    }

    public async Task<Guid> CreateAsync(SaveAttributeCommand command, CancellationToken cancellationToken = default)
    {
        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command.Name, command.Slug), null, cancellationToken);
        var attr = ProductAttribute.Create(command.Name, slug, command.SortOrder);
        ApplyValues(attr, command.Values);
        _db.Attributes.Add(attr);
        await _db.SaveChangesAsync(cancellationToken);
        return attr.Id;
    }

    public async Task UpdateAsync(Guid id, SaveAttributeCommand command, CancellationToken cancellationToken = default)
    {
        var attr = await _db.Attributes
            .Include(a => a.Values)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductAttribute), id);

        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command.Name, command.Slug), id, cancellationToken);
        attr.Update(command.Name, slug, command.SortOrder);

        var existingIds = command.Values?.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToHashSet() ?? [];
        var toRemove = attr.Values.Where(v => !existingIds.Contains(v.Id)).ToList();
        _db.AttributeValues.RemoveRange(toRemove);

        ApplyValues(attr, command.Values);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var attr = await _db.Attributes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductAttribute), id);
        _db.Attributes.Remove(attr);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private void ApplyValues(ProductAttribute attr, IReadOnlyList<SaveAttributeValueCommand>? values)
    {
        if (values is null or { Count: 0 })
            return;

        foreach (var v in values)
        {
            var valueSlug = SlugGenerator.FromTitle(v.Slug ?? string.Empty);
            if (string.IsNullOrWhiteSpace(valueSlug))
                valueSlug = SlugGenerator.FromTitle(v.Value);

            if (v.Id.HasValue)
            {
                var existing = attr.Values.FirstOrDefault(x => x.Id == v.Id.Value);
                existing?.Update(v.Value, valueSlug, v.SortOrder);
            }
            else
            {
                attr.AddValue(v.Value, valueSlug, v.SortOrder);
            }
        }
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken)
    {
        var candidate = slug;
        var suffix = 2;
        while (await _db.Attributes.AnyAsync(a => a.Slug == candidate && (!excludeId.HasValue || a.Id != excludeId.Value), cancellationToken))
        {
            candidate = $"{slug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static string ResolveSlug(string name, string? slug)
    {
        var resolved = SlugGenerator.FromTitle(slug ?? string.Empty);
        if (string.IsNullOrWhiteSpace(resolved))
            resolved = SlugGenerator.FromTitle(name);
        if (string.IsNullOrWhiteSpace(resolved))
            throw new DomainException("نامک ویژگی الزامی است.");
        return resolved;
    }

    private static AttributeListItemDto Map(ProductAttribute attr) =>
        new(
            attr.Id,
            attr.Name,
            attr.Slug,
            attr.SortOrder,
            attr.Values
                .OrderBy(v => v.SortOrder)
                .Select(v => new AttributeValueDto(v.Id, v.Value, v.Slug, v.SortOrder))
                .ToList());
}

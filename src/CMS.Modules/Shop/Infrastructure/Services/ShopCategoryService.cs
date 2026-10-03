using CMS.Application.Security;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Categories;
using CMS.Modules.Shop.Application.Common;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class ShopCategoryService : IShopCategoryService
{
    private readonly ShopDbContext _db;
    private readonly IValidator<SaveShopCategoryCommand> _validator;
    private readonly IHtmlContentSanitizer _htmlSanitizer;

    public ShopCategoryService(
        ShopDbContext db,
        IValidator<SaveShopCategoryCommand> validator,
        IHtmlContentSanitizer htmlSanitizer)
    {
        _db = db;
        _validator = validator;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<IReadOnlyList<ShopCategoryListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new ShopCategoryListItemDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Products.Count,
                c.ImageUrl,
                c.SortOrder,
                c.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ShopCategoryDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ShopCategoryDetailDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.Content,
                c.ImageUrl,
                c.MetaTitle,
                c.MetaDescription,
                c.SeoKeywords,
                c.SortOrder,
                c.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(SaveShopCategoryCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), null, cancellationToken);
        var content = SanitizeContent(command.Content);
        var category = ShopCategory.Create(
            command.Name,
            slug,
            command.Description,
            content,
            command.ImageUrl,
            command.MetaTitle,
            command.MetaDescription,
            command.SeoKeywords,
            command.SortOrder,
            command.IsActive);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return category.Id;
    }

    public async Task UpdateAsync(Guid id, SaveShopCategoryCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ShopCategory), id);

        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), id, cancellationToken);
        var content = SanitizeContent(command.Content);
        category.Update(
            command.Name,
            slug,
            command.Description,
            content,
            command.ImageUrl,
            command.MetaTitle,
            command.MetaDescription,
            command.SeoKeywords,
            command.SortOrder,
            command.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ShopCategory), id);

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private string? SanitizeContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var sanitized = _htmlSanitizer.Sanitize(content);
        return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
    }

    private async Task ValidateAsync(SaveShopCategoryCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static string ResolveSlug(SaveShopCategoryCommand command)
    {
        var slug = SlugGenerator.FromTitle(command.Slug ?? string.Empty);
        if (string.IsNullOrWhiteSpace(slug))
            slug = SlugGenerator.FromTitle(command.Name);

        if (string.IsNullOrWhiteSpace(slug))
            slug = $"category-{Guid.NewGuid():N}"[..16];

        return slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken)
    {
        var candidate = slug;
        var suffix = 2;
        while (await _db.Categories.AnyAsync(
                   c => c.Slug == candidate && (!excludeId.HasValue || c.Id != excludeId.Value),
                   cancellationToken))
        {
            candidate = $"{slug}-{suffix}";
            suffix++;
        }

        return candidate;
    }
}

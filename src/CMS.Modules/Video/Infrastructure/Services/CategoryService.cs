using CMS.Application.Security;
using CMS.Domain.Exceptions;
using CMS.Modules.Video.Application.Categories;
using CMS.Modules.Video.Application.Common;
using CMS.Modules.Video.Application.Interfaces;
using CMS.Modules.Video.Domain.Entities;
using CMS.Modules.Video.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Video.Infrastructure.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly VideoDbContext _db;
    private readonly IValidator<SaveCategoryCommand> _validator;
    private readonly IHtmlContentSanitizer _htmlSanitizer;

    public CategoryService(
        VideoDbContext db,
        IValidator<SaveCategoryCommand> validator,
        IHtmlContentSanitizer htmlSanitizer)
    {
        _db = db;
        _validator = validator;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<IReadOnlyList<CategoryListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryListItemDto(
                c.Id,
                c.Name,
                c.Slug,
                c.VideoItems.Count,
                c.ImageUrl))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDetailDto(c.Id, c.Name, c.Slug, c.Description, c.ImageUrl))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(SaveCategoryCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), null, cancellationToken);
        var description = _htmlSanitizer.Sanitize(command.Description);
        var category = Category.Create(command.Name, slug, description, command.ImageUrl);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return category.Id;
    }

    public async Task UpdateAsync(Guid id, SaveCategoryCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), id);

        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), id, cancellationToken);
        var description = _htmlSanitizer.Sanitize(command.Description);
        category.Update(command.Name, slug, description, command.ImageUrl);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), id);

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateAsync(SaveCategoryCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static string ResolveSlug(SaveCategoryCommand command)
    {
        var slug = SlugGenerator.FromTitle(command.Slug ?? string.Empty, 200);
        if (string.IsNullOrWhiteSpace(slug))
            slug = SlugGenerator.FromTitle(command.Name, 200);

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                [nameof(command.Slug)] = ["نامک الزامی است."]
            });

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

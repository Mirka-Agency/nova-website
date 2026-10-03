using CMS.Application.Common.Paging;
using CMS.Application.Security;
using CMS.Domain.Exceptions;
using CMS.Modules.Services.Application.Common;
using CMS.Modules.Services.Application.Interfaces;
using CMS.Modules.Services.Application.ServiceItems;
using CMS.Modules.Services.Domain.Entities;
using CMS.Modules.Services.Domain.Enums;
using CMS.Modules.Services.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Services.Infrastructure.Services;

public sealed class ServiceItemService : IServiceItemService
{
    private readonly ServicesDbContext _db;
    private readonly IValidator<SaveServiceItemCommand> _validator;
    private readonly IHtmlContentSanitizer _htmlSanitizer;

    public ServiceItemService(
        ServicesDbContext db,
        IValidator<SaveServiceItemCommand> validator,
        IHtmlContentSanitizer htmlSanitizer)
    {
        _db = db;
        _validator = validator;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<IReadOnlyList<ServiceItemListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(new ServiceItemListRequest { Page = 1, PageSize = PagedRequest.MaxPageSize }, cancellationToken);
        return page.Items;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.ServiceItems.AsNoTracking().CountAsync(cancellationToken);

    public async Task<PagedResult<ServiceItemListItemDto>> ListPagedAsync(
        ServiceItemListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilterQuery(request);

        var total = await query.CountAsync(cancellationToken);
        var items = await ApplySort(query, request.Sort)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(p => new ServiceItemListItemDto(
                p.Id,
                p.Title,
                p.Slug,
                p.Status,
                p.Category != null ? p.Category.Name : null,
                p.CreatedAtUtc,
                p.PublishedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceItemListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    private IQueryable<ServiceItem> BuildFilterQuery(ServiceItemListRequest request)
    {
        var query = _db.ServiceItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p =>
                p.Title.Contains(term) ||
                p.Slug.Contains(term) ||
                (p.Category != null && p.Category.Name.Contains(term)));
        }

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        if (request.FromUtc.HasValue)
            query = query.Where(p => p.CreatedAtUtc >= request.FromUtc.Value);

        if (request.ToUtc.HasValue)
            query = query.Where(p => p.CreatedAtUtc <= request.ToUtc.Value);

        return query;
    }

    private static IQueryable<ServiceItem> ApplySort(IQueryable<ServiceItem> query, string? sort) =>
        string.Equals(sort, "oldest", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(p => p.CreatedAtUtc)
            : string.Equals(sort, "title", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(p => p.Title)
                : string.Equals(sort, "title_desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(p => p.Title)
                    : string.Equals(sort, "published", StringComparison.OrdinalIgnoreCase)
                        ? query.OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
                        : query.OrderByDescending(p => p.CreatedAtUtc);

    public async Task<ServiceItemDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.ServiceItems
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (item is null)
            return null;

        return MapDetail(item);
    }

    public async Task<Guid?> FindUserDraftIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _db.ServiceItems
            .AsNoTracking()
            .Where(p => p.OwnedByUserId == userId && p.Status == ServiceStatus.Draft)
            .OrderByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> GetOrCreateUserDraftAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("شناسه کاربر برای پیش‌نویس الزامی است.");

        var existingId = await FindUserDraftIdAsync(userId, cancellationToken);
        if (existingId.HasValue)
            return existingId.Value;

        var slugSeed = $"draft-{Guid.NewGuid():N}"[..20];
        var slug = await EnsureUniqueSlugAsync(slugSeed, null, cancellationToken);
        var item = ServiceItem.Create(
            ServiceItemDraftDefaults.Title,
            slug,
            string.Empty,
            excerpt: null,
            categoryId: null,
            coverImageUrl: null,
            iconUrl: null,
            authorUserId: null,
            authorDisplayName: null,
            metaTitle: null,
            metaDescription: null,
            seoKeywords: null,
            canonicalUrl: null,
            ogTitle: null,
            ogDescription: null,
            ogImageUrl: null);
        item.SetOwnedBy(userId);

        _db.ServiceItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task<Guid> DiscardUserDraftAsync(string userId, Guid draftId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("شناسه کاربر برای پیش‌نویس الزامی است.");

        var item = await _db.ServiceItems.FirstOrDefaultAsync(p => p.Id == draftId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceItem), draftId);

        if (item.Status != ServiceStatus.Draft
            || !string.Equals(item.OwnedByUserId, userId, StringComparison.Ordinal))
        {
            throw new DomainException("این پیش‌نویس قابل حذف نیست.");
        }

        _db.ServiceItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetOrCreateUserDraftAsync(userId, cancellationToken);
    }

    public async Task<Guid> CreateAsync(SaveServiceItemCommand command, CancellationToken cancellationToken = default)
    {
        command = Normalize(command);
        await ValidateAsync(command, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), null, cancellationToken);
        await EnsureCategoryExistsAsync(command.CategoryId, cancellationToken);

        var sanitizedBody = _htmlSanitizer.Sanitize(command.Body);
        var item = ServiceItem.Create(
            command.Title,
            slug,
            sanitizedBody,
            command.Excerpt,
            command.CategoryId,
            command.CoverImageUrl,
            iconUrl: null,
            command.AuthorUserId,
            command.AuthorDisplayName,
            command.MetaTitle,
            command.MetaDescription,
            command.SeoKeywords,
            command.CanonicalUrl,
            command.OgTitle,
            command.OgDescription,
            command.OgImageUrl);

        ApplyPublishState(item, command);

        _db.ServiceItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task UpdateAsync(Guid id, SaveServiceItemCommand command, CancellationToken cancellationToken = default)
    {
        command = Normalize(command);
        await ValidateAsync(command, cancellationToken);

        var item = await _db.ServiceItems
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceItem), id);

        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), id, cancellationToken);
        await EnsureCategoryExistsAsync(command.CategoryId, cancellationToken);

        var sanitizedBody = _htmlSanitizer.Sanitize(command.Body);
        item.Update(
            command.Title,
            slug,
            sanitizedBody,
            command.Excerpt,
            command.CategoryId,
            command.CoverImageUrl,
            iconUrl: null,
            command.AuthorUserId,
            command.AuthorDisplayName,
            command.MetaTitle,
            command.MetaDescription,
            command.SeoKeywords,
            command.CanonicalUrl,
            command.OgTitle,
            command.OgDescription,
            command.OgImageUrl);

        ApplyPublishState(item, command);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.ServiceItems.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceItem), id);

        _db.ServiceItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static ServiceItemDetailDto MapDetail(ServiceItem item) =>
        new(
            item.Id,
            item.Title,
            item.Slug,
            item.Body,
            item.Excerpt,
            item.Status,
            item.CoverImageUrl,
            item.CategoryId,
            item.AuthorUserId,
            item.AuthorDisplayName,
            item.PublishedAtUtc,
            item.MetaTitle,
            item.MetaDescription,
            item.SeoKeywords,
            item.CanonicalUrl,
            item.OgTitle,
            item.OgDescription,
            item.OgImageUrl,
            item.CreatedAtUtc);

    private static SaveServiceItemCommand Normalize(SaveServiceItemCommand command)
    {
        var title = string.IsNullOrWhiteSpace(command.Title)
            ? ServiceItemDraftDefaults.Title
            : command.Title.Trim();
        var body = command.Body ?? string.Empty;
        return command with { Title = title, Body = body };
    }

    private static void ApplyPublishState(ServiceItem item, SaveServiceItemCommand command)
    {
        if (command.Publish)
            item.Publish(command.PublishedAtUtc);
        else
        {
            item.Unpublish();
            if (command.PublishedAtUtc.HasValue)
                item.SetPublishedAt(command.PublishedAtUtc);
        }
    }

    private async Task ValidateAsync(SaveServiceItemCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static string ResolveSlug(SaveServiceItemCommand command)
    {
        var slug = SlugGenerator.FromTitle(command.Slug ?? string.Empty);
        if (string.IsNullOrWhiteSpace(slug))
            slug = SlugGenerator.FromTitle(command.Title);

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
        while (await _db.ServiceItems.AnyAsync(
                   p => p.Slug == candidate && (!excludeId.HasValue || p.Id != excludeId.Value),
                   cancellationToken))
        {
            candidate = $"{slug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private async Task EnsureCategoryExistsAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue)
            return;

        var exists = await _db.Categories.AnyAsync(c => c.Id == categoryId.Value, cancellationToken);
        if (!exists)
            throw new NotFoundException(nameof(Category), categoryId.Value);
    }
}

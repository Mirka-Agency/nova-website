using CMS.Application.Common.Paging;
using CMS.Application.Security;
using CMS.Domain.Exceptions;
using CMS.Modules.Video.Application.Common;
using CMS.Modules.Video.Application.Interfaces;
using CMS.Modules.Video.Application.VideoItems;
using CMS.Modules.Video.Domain.Entities;
using CMS.Modules.Video.Domain.Enums;
using CMS.Modules.Video.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Video.Infrastructure.Services;

public sealed class VideoItemService : IVideoItemService
{
    private readonly VideoDbContext _db;
    private readonly IValidator<SaveVideoItemCommand> _validator;
    private readonly IHtmlContentSanitizer _htmlSanitizer;

    public VideoItemService(
        VideoDbContext db,
        IValidator<SaveVideoItemCommand> validator,
        IHtmlContentSanitizer htmlSanitizer)
    {
        _db = db;
        _validator = validator;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<IReadOnlyList<VideoItemListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(new VideoItemListRequest { Page = 1, PageSize = PagedRequest.MaxPageSize }, cancellationToken);
        return page.Items;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.VideoItems.AsNoTracking().CountAsync(cancellationToken);

    public async Task<PagedResult<VideoItemListItemDto>> ListPagedAsync(
        VideoItemListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilterQuery(request);

        var total = await query.CountAsync(cancellationToken);
        var items = await ApplySort(query, request.Sort)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(p => new VideoItemListItemDto(
                p.Id,
                p.Title,
                p.Slug,
                p.Status,
                p.Category != null ? p.Category.Name : null,
                p.CreatedAtUtc,
                p.PublishedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<VideoItemListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    private IQueryable<VideoItem> BuildFilterQuery(VideoItemListRequest request)
    {
        var query = _db.VideoItems.AsNoTracking();

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

    private static IQueryable<VideoItem> ApplySort(IQueryable<VideoItem> query, string? sort) =>
        string.Equals(sort, "oldest", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(p => p.CreatedAtUtc)
            : string.Equals(sort, "title", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(p => p.Title)
                : string.Equals(sort, "title_desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(p => p.Title)
                    : string.Equals(sort, "published", StringComparison.OrdinalIgnoreCase)
                        ? query.OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
                        : query.OrderByDescending(p => p.CreatedAtUtc);

    public async Task<VideoItemDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.VideoItems
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

        return await _db.VideoItems
            .AsNoTracking()
            .Where(p => p.OwnedByUserId == userId && p.Status == VideoStatus.Draft)
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
        var item = VideoItem.Create(
            VideoItemDraftDefaults.Title,
            slug,
            string.Empty,
            excerpt: null,
            videoUrl: null,
            categoryId: null,
            coverImageUrl: null,
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

        _db.VideoItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task<Guid> DiscardUserDraftAsync(string userId, Guid draftId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("شناسه کاربر برای پیش‌نویس الزامی است.");

        var item = await _db.VideoItems.FirstOrDefaultAsync(p => p.Id == draftId, cancellationToken)
            ?? throw new NotFoundException(nameof(VideoItem), draftId);

        if (item.Status != VideoStatus.Draft
            || !string.Equals(item.OwnedByUserId, userId, StringComparison.Ordinal))
        {
            throw new DomainException("این پیش‌نویس قابل حذف نیست.");
        }

        _db.VideoItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetOrCreateUserDraftAsync(userId, cancellationToken);
    }

    public async Task<Guid> CreateAsync(SaveVideoItemCommand command, CancellationToken cancellationToken = default)
    {
        command = Normalize(command);
        await ValidateAsync(command, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), null, cancellationToken);
        await EnsureCategoryExistsAsync(command.CategoryId, cancellationToken);

        var sanitizedBody = _htmlSanitizer.Sanitize(command.Body);
        var item = VideoItem.Create(
            command.Title,
            slug,
            sanitizedBody,
            command.Excerpt,
            command.VideoUrl,
            command.CategoryId,
            command.CoverImageUrl,
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

        _db.VideoItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task UpdateAsync(Guid id, SaveVideoItemCommand command, CancellationToken cancellationToken = default)
    {
        command = Normalize(command);
        await ValidateAsync(command, cancellationToken);

        var item = await _db.VideoItems
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(VideoItem), id);

        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), id, cancellationToken);
        await EnsureCategoryExistsAsync(command.CategoryId, cancellationToken);

        var sanitizedBody = _htmlSanitizer.Sanitize(command.Body);
        item.Update(
            command.Title,
            slug,
            sanitizedBody,
            command.Excerpt,
            command.VideoUrl,
            command.CategoryId,
            command.CoverImageUrl,
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
        var item = await _db.VideoItems.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(VideoItem), id);

        _db.VideoItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static VideoItemDetailDto MapDetail(VideoItem item) =>
        new(
            item.Id,
            item.Title,
            item.Slug,
            item.Body,
            item.Excerpt,
            item.VideoUrl,
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

    private static SaveVideoItemCommand Normalize(SaveVideoItemCommand command)
    {
        var title = string.IsNullOrWhiteSpace(command.Title)
            ? VideoItemDraftDefaults.Title
            : command.Title.Trim();
        var body = command.Body ?? string.Empty;
        return command with { Title = title, Body = body };
    }

    private static void ApplyPublishState(VideoItem item, SaveVideoItemCommand command)
    {
        // Null means "leave publish state alone" (autosave / content-only updates).
        if (command.Publish is null)
            return;

        if (command.Publish.Value)
            item.Publish(command.PublishedAtUtc);
        else
        {
            item.Unpublish();
            if (command.PublishedAtUtc.HasValue)
                item.SetPublishedAt(command.PublishedAtUtc);
        }
    }

    private async Task ValidateAsync(SaveVideoItemCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static string ResolveSlug(SaveVideoItemCommand command)
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
        while (await _db.VideoItems.AnyAsync(
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

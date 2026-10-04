using CMS.Application.Common.Paging;
using CMS.Application.Security;
using CMS.Domain.Exceptions;
using CMS.Modules.News.Application.Articles;
using CMS.Modules.News.Application.Common;
using CMS.Modules.News.Application.Interfaces;
using CMS.Modules.News.Domain.Entities;
using CMS.Modules.News.Domain.Enums;
using CMS.Modules.News.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.News.Infrastructure.Services;

public sealed class ArticleService : IArticleService
{
    private readonly NewsDbContext _db;
    private readonly IValidator<SaveArticleCommand> _validator;
    private readonly IHtmlContentSanitizer _htmlSanitizer;

    public ArticleService(
        NewsDbContext db,
        IValidator<SaveArticleCommand> validator,
        IHtmlContentSanitizer htmlSanitizer)
    {
        _db = db;
        _validator = validator;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<IReadOnlyList<ArticleListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(new ArticleListRequest { Page = 1, PageSize = PagedRequest.MaxPageSize }, cancellationToken);
        return page.Items;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.Articles.AsNoTracking().CountAsync(cancellationToken);

    public async Task<PagedResult<ArticleListItemDto>> ListPagedAsync(
        ArticleListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilterQuery(request);

        var total = await query.CountAsync(cancellationToken);
        var items = await ApplySort(query, request.Sort)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(p => new ArticleListItemDto(
                p.Id,
                p.Title,
                p.Slug,
                p.Status,
                p.Kind,
                p.Category != null ? p.Category.Name : null,
                p.CreatedAtUtc,
                p.PublishedAtUtc,
                p.EventStartAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<ArticleListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    private IQueryable<Article> BuildFilterQuery(ArticleListRequest request)
    {
        var query = _db.Articles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p =>
                p.Title.Contains(term) ||
                p.Slug.Contains(term) ||
                (p.Category != null && p.Category.Name.Contains(term)) ||
                (p.Location != null && p.Location.Contains(term)));
        }

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (request.Kind.HasValue)
            query = query.Where(p => p.Kind == request.Kind.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        if (request.FromUtc.HasValue)
            query = query.Where(p => p.CreatedAtUtc >= request.FromUtc.Value);

        if (request.ToUtc.HasValue)
            query = query.Where(p => p.CreatedAtUtc <= request.ToUtc.Value);

        if (request.EventFromUtc.HasValue)
            query = query.Where(p => p.EventStartAtUtc.HasValue && p.EventStartAtUtc >= request.EventFromUtc.Value);

        if (request.EventToUtc.HasValue)
            query = query.Where(p => p.EventStartAtUtc.HasValue && p.EventStartAtUtc <= request.EventToUtc.Value);

        return query;
    }

    private static IQueryable<Article> ApplySort(IQueryable<Article> query, string? sort) =>
        string.Equals(sort, "oldest", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(p => p.CreatedAtUtc)
            : string.Equals(sort, "title", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(p => p.Title)
                : string.Equals(sort, "title_desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(p => p.Title)
                    : string.Equals(sort, "published", StringComparison.OrdinalIgnoreCase)
                        ? query.OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
                        : string.Equals(sort, "event_start", StringComparison.OrdinalIgnoreCase)
                            ? query.OrderBy(p => p.EventStartAtUtc ?? DateTime.MaxValue)
                            : string.Equals(sort, "event_start_desc", StringComparison.OrdinalIgnoreCase)
                                ? query.OrderByDescending(p => p.EventStartAtUtc ?? DateTime.MinValue)
                                : query.OrderByDescending(p => p.CreatedAtUtc);

    public async Task<ArticleDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var article = await _db.Articles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (article is null)
            return null;

        return MapDetail(article);
    }

    public async Task<Guid?> FindUserDraftIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _db.Articles
            .AsNoTracking()
            .Where(p => p.OwnedByUserId == userId && p.Status == ArticleStatus.Draft)
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
        var article = Article.Create(
            ArticleDraftDefaults.Title,
            slug,
            string.Empty,
            excerpt: null,
            kind: ArticleKind.News,
            categoryId: null,
            coverImageUrl: null,
            authorUserId: null,
            authorDisplayName: null,
            eventStartAtUtc: null,
            eventEndAtUtc: null,
            location: null,
            metaTitle: null,
            metaDescription: null,
            seoKeywords: null,
            canonicalUrl: null,
            ogTitle: null,
            ogDescription: null,
            ogImageUrl: null);
        article.SetOwnedBy(userId);

        _db.Articles.Add(article);
        await _db.SaveChangesAsync(cancellationToken);
        return article.Id;
    }

    public async Task<Guid> DiscardUserDraftAsync(string userId, Guid draftId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("شناسه کاربر برای پیش‌نویس الزامی است.");

        var article = await _db.Articles.FirstOrDefaultAsync(p => p.Id == draftId, cancellationToken)
            ?? throw new NotFoundException(nameof(Article), draftId);

        if (article.Status != ArticleStatus.Draft
            || !string.Equals(article.OwnedByUserId, userId, StringComparison.Ordinal))
        {
            throw new DomainException("این پیش‌نویس قابل حذف نیست.");
        }

        _db.Articles.Remove(article);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetOrCreateUserDraftAsync(userId, cancellationToken);
    }

    public async Task<Guid> CreateAsync(SaveArticleCommand command, CancellationToken cancellationToken = default)
    {
        command = Normalize(command);
        await ValidateAsync(command, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), null, cancellationToken);
        await EnsureCategoryExistsAsync(command.CategoryId, cancellationToken);

        var sanitizedBody = _htmlSanitizer.Sanitize(command.Body);
        var galleryJson = ArticleGalleryJson.NormalizeJson(command.GalleryJson);
        var eventInfoJson = command.Kind == ArticleKind.Event
            ? ArticleEventInfoJson.NormalizeJson(command.EventInfoJson)
            : null;
        var article = Article.Create(
            command.Title,
            slug,
            sanitizedBody,
            command.Excerpt,
            command.Kind,
            command.CategoryId,
            command.CoverImageUrl,
            command.AuthorUserId,
            command.AuthorDisplayName,
            command.EventStartAtUtc,
            command.EventEndAtUtc,
            command.Location,
            command.MetaTitle,
            command.MetaDescription,
            command.SeoKeywords,
            command.CanonicalUrl,
            command.OgTitle,
            command.OgDescription,
            command.OgImageUrl);
        article.SetGalleryJson(galleryJson);
        article.SetEventInfoJson(eventInfoJson);
        article.SetAttachment(command.AttachmentUrl, command.AttachmentFileName);

        ApplyPublishState(article, command);

        _db.Articles.Add(article);
        await _db.SaveChangesAsync(cancellationToken);
        return article.Id;
    }

    public async Task UpdateAsync(Guid id, SaveArticleCommand command, CancellationToken cancellationToken = default)
    {
        command = Normalize(command);
        await ValidateAsync(command, cancellationToken);

        var article = await _db.Articles
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Article), id);

        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), id, cancellationToken);
        await EnsureCategoryExistsAsync(command.CategoryId, cancellationToken);

        var sanitizedBody = _htmlSanitizer.Sanitize(command.Body);
        var galleryJson = ArticleGalleryJson.NormalizeJson(command.GalleryJson);
        var eventInfoJson = command.Kind == ArticleKind.Event
            ? ArticleEventInfoJson.NormalizeJson(command.EventInfoJson)
            : null;
        article.Update(
            command.Title,
            slug,
            sanitizedBody,
            command.Excerpt,
            command.Kind,
            command.CategoryId,
            command.CoverImageUrl,
            command.AuthorUserId,
            command.AuthorDisplayName,
            command.EventStartAtUtc,
            command.EventEndAtUtc,
            command.Location,
            command.MetaTitle,
            command.MetaDescription,
            command.SeoKeywords,
            command.CanonicalUrl,
            command.OgTitle,
            command.OgDescription,
            command.OgImageUrl);
        article.SetGalleryJson(galleryJson);
        article.SetEventInfoJson(eventInfoJson);
        article.SetAttachment(command.AttachmentUrl, command.AttachmentFileName);

        ApplyPublishState(article, command);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Article), id);

        _db.Articles.Remove(article);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static ArticleDetailDto MapDetail(Article article) =>
        new(
            article.Id,
            article.Title,
            article.Slug,
            article.Body,
            article.Excerpt,
            article.Status,
            article.Kind,
            article.CoverImageUrl,
            article.GalleryJson,
            article.AttachmentUrl,
            article.AttachmentFileName,
            article.CategoryId,
            article.AuthorUserId,
            article.AuthorDisplayName,
            article.PublishedAtUtc,
            article.EventStartAtUtc,
            article.EventEndAtUtc,
            article.Location,
            article.EventInfoJson,
            article.MetaTitle,
            article.MetaDescription,
            article.SeoKeywords,
            article.CanonicalUrl,
            article.OgTitle,
            article.OgDescription,
            article.OgImageUrl,
            article.CreatedAtUtc);

    private static SaveArticleCommand Normalize(SaveArticleCommand command)
    {
        var title = string.IsNullOrWhiteSpace(command.Title)
            ? ArticleDraftDefaults.Title
            : command.Title.Trim();
        var body = command.Body ?? string.Empty;
        return command with
        {
            Title = title,
            Body = body,
            GalleryJson = ArticleGalleryJson.NormalizeJson(command.GalleryJson),
            EventInfoJson = command.Kind == ArticleKind.Event
                ? ArticleEventInfoJson.NormalizeJson(command.EventInfoJson)
                : null
        };
    }

    private static void ApplyPublishState(Article article, SaveArticleCommand command)
    {
        // Null means "leave publish state alone" (autosave / content-only updates).
        if (command.Publish is null)
            return;

        if (command.Publish.Value)
            article.Publish(command.PublishedAtUtc);
        else
        {
            article.Unpublish();
            if (command.PublishedAtUtc.HasValue)
                article.SetPublishedAt(command.PublishedAtUtc);
        }
    }

    private async Task ValidateAsync(SaveArticleCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static string ResolveSlug(SaveArticleCommand command)
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
        while (await _db.Articles.AnyAsync(
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

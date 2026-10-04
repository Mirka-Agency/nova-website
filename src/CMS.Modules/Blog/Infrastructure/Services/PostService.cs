using CMS.Application.Common.Paging;
using CMS.Application.Security;
using CMS.Domain.Exceptions;
using CMS.Modules.Blog.Application.Common;
using CMS.Modules.Blog.Application.Interfaces;
using CMS.Modules.Blog.Application.Posts;
using CMS.Modules.Blog.Domain.Entities;
using CMS.Modules.Blog.Domain.Enums;
using CMS.Modules.Blog.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Blog.Infrastructure.Services;

public sealed class PostService : IPostService
{
    private readonly BlogDbContext _db;
    private readonly IValidator<SavePostCommand> _validator;
    private readonly IHtmlContentSanitizer _htmlSanitizer;

    public PostService(
        BlogDbContext db,
        IValidator<SavePostCommand> validator,
        IHtmlContentSanitizer htmlSanitizer)
    {
        _db = db;
        _validator = validator;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<IReadOnlyList<PostListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(new PostListRequest { Page = 1, PageSize = PagedRequest.MaxPageSize }, cancellationToken);
        return page.Items;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.Posts.AsNoTracking().CountAsync(cancellationToken);

    public async Task<PagedResult<PostListItemDto>> ListPagedAsync(
        PostListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilterQuery(request);

        var total = await query.CountAsync(cancellationToken);
        var items = await ApplySort(query, request.Sort)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(p => new PostListItemDto(
                p.Id,
                p.Title,
                p.Slug,
                p.Status,
                p.Category != null ? p.Category.Name : null,
                p.CreatedAtUtc,
                p.PublishedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<PostListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    private IQueryable<Post> BuildFilterQuery(PostListRequest request)
    {
        var query = _db.Posts.AsNoTracking();

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

    private static IQueryable<Post> ApplySort(IQueryable<Post> query, string? sort) =>
        string.Equals(sort, "oldest", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(p => p.CreatedAtUtc)
            : string.Equals(sort, "title", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(p => p.Title)
                : string.Equals(sort, "title_desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(p => p.Title)
                    : string.Equals(sort, "published", StringComparison.OrdinalIgnoreCase)
                        ? query.OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
                        : query.OrderByDescending(p => p.CreatedAtUtc);

    public async Task<PostDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var post = await _db.Posts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (post is null)
            return null;

        return MapDetail(post);
    }

    public async Task<Guid?> FindUserDraftIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _db.Posts
            .AsNoTracking()
            .Where(p => p.OwnedByUserId == userId && p.Status == PostStatus.Draft)
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
        var post = Post.Create(
            PostDraftDefaults.Title,
            slug,
            string.Empty,
            excerpt: null,
            categoryId: null,
            coverImageUrl: null,
            coverVideoUrl: null,
            authorUserId: null,
            authorDisplayName: null,
            metaTitle: null,
            metaDescription: null,
            seoKeywords: null,
            canonicalUrl: null,
            ogTitle: null,
            ogDescription: null,
            ogImageUrl: null,
            faqJson: null);
        post.SetOwnedBy(userId);

        _db.Posts.Add(post);
        await _db.SaveChangesAsync(cancellationToken);
        return post.Id;
    }

    public async Task<Guid> DiscardUserDraftAsync(string userId, Guid draftId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("شناسه کاربر برای پیش‌نویس الزامی است.");

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == draftId, cancellationToken)
            ?? throw new NotFoundException(nameof(Post), draftId);

        if (post.Status != PostStatus.Draft
            || !string.Equals(post.OwnedByUserId, userId, StringComparison.Ordinal))
        {
            throw new DomainException("این پیش‌نویس قابل حذف نیست.");
        }

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetOrCreateUserDraftAsync(userId, cancellationToken);
    }

    public async Task<Guid> CreateAsync(SavePostCommand command, CancellationToken cancellationToken = default)
    {
        command = Normalize(command);
        await ValidateAsync(command, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), null, cancellationToken);
        await EnsureCategoryExistsAsync(command.CategoryId, cancellationToken);

        var sanitizedBody = _htmlSanitizer.Sanitize(command.Body);
        var faqJson = PostFaqJson.NormalizeJson(command.FaqJson);
        var post = Post.Create(
            command.Title,
            slug,
            sanitizedBody,
            command.Excerpt,
            command.CategoryId,
            command.CoverImageUrl,
            command.CoverVideoUrl,
            command.AuthorUserId,
            command.AuthorDisplayName,
            command.MetaTitle,
            command.MetaDescription,
            command.SeoKeywords,
            command.CanonicalUrl,
            command.OgTitle,
            command.OgDescription,
            command.OgImageUrl,
            faqJson);

        ApplyPublishState(post, command);

        _db.Posts.Add(post);
        await _db.SaveChangesAsync(cancellationToken);
        return post.Id;
    }

    public async Task UpdateAsync(Guid id, SavePostCommand command, CancellationToken cancellationToken = default)
    {
        command = Normalize(command);
        await ValidateAsync(command, cancellationToken);

        var post = await _db.Posts
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Post), id);

        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), id, cancellationToken);
        await EnsureCategoryExistsAsync(command.CategoryId, cancellationToken);

        var sanitizedBody = _htmlSanitizer.Sanitize(command.Body);
        var faqJson = PostFaqJson.NormalizeJson(command.FaqJson);
        post.Update(
            command.Title,
            slug,
            sanitizedBody,
            command.Excerpt,
            command.CategoryId,
            command.CoverImageUrl,
            command.CoverVideoUrl,
            command.AuthorUserId,
            command.AuthorDisplayName,
            command.MetaTitle,
            command.MetaDescription,
            command.SeoKeywords,
            command.CanonicalUrl,
            command.OgTitle,
            command.OgDescription,
            command.OgImageUrl,
            faqJson);

        ApplyPublishState(post, command);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Post), id);

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static PostDetailDto MapDetail(Post post) =>
        new(
            post.Id,
            post.Title,
            post.Slug,
            post.Body,
            post.Excerpt,
            post.Status,
            post.CoverImageUrl,
            post.CoverVideoUrl,
            post.CategoryId,
            post.AuthorUserId,
            post.AuthorDisplayName,
            post.PublishedAtUtc,
            post.MetaTitle,
            post.MetaDescription,
            post.SeoKeywords,
            post.CanonicalUrl,
            post.OgTitle,
            post.OgDescription,
            post.OgImageUrl,
            post.FaqJson,
            post.CreatedAtUtc);

    private static SavePostCommand Normalize(SavePostCommand command)
    {
        var title = string.IsNullOrWhiteSpace(command.Title)
            ? PostDraftDefaults.Title
            : command.Title.Trim();
        var body = command.Body ?? string.Empty;
        return command with { Title = title, Body = body };
    }

    private static void ApplyPublishState(Post post, SavePostCommand command)
    {
        // Null means "leave publish state alone" (autosave / content-only updates).
        if (command.Publish is null)
            return;

        if (command.Publish.Value)
            post.Publish(command.PublishedAtUtc);
        else
        {
            post.Unpublish();
            if (command.PublishedAtUtc.HasValue)
                post.SetPublishedAt(command.PublishedAtUtc);
        }
    }

    private async Task ValidateAsync(SavePostCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static string ResolveSlug(SavePostCommand command)
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
        while (await _db.Posts.AnyAsync(
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

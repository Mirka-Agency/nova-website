using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Blog.Domain.Enums;

namespace CMS.Modules.Blog.Domain.Entities;

public class Post : BaseEntity
{
    private Post()
    {
    }

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Excerpt { get; private set; }
    public PostStatus Status { get; private set; } = PostStatus.Draft;
    public string? CoverImageUrl { get; private set; }
    public string? CoverImageAlt { get; private set; }
    public string? CoverVideoUrl { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public string? AuthorUserId { get; private set; }
    public string? AuthorDisplayName { get; private set; }
    /// <summary>Identity user who owns this post for per-user auto-drafts.</summary>
    public string? OwnedByUserId { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? SeoKeywords { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public string? OgTitle { get; private set; }
    public string? OgDescription { get; private set; }
    public string? OgImageUrl { get; private set; }
    /// <summary>JSON array of FAQ items: [{"question":"...","answer":"..."}].</summary>
    public string? FaqJson { get; private set; }

    public static Post Create(
        string title,
        string slug,
        string body,
        string? excerpt,
        Guid? categoryId,
        string? coverImageUrl,
        string? coverImageAlt,
        string? coverVideoUrl,
        string? authorUserId,
        string? authorDisplayName,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? faqJson)
    {
        var post = new Post();
        post.ApplyContent(
            title, slug, body, excerpt, categoryId, coverImageUrl, coverImageAlt, coverVideoUrl,
            authorUserId, authorDisplayName,
            metaTitle, metaDescription, seoKeywords, canonicalUrl,
            ogTitle, ogDescription, ogImageUrl, faqJson);
        return post;
    }

    public void Update(
        string title,
        string slug,
        string body,
        string? excerpt,
        Guid? categoryId,
        string? coverImageUrl,
        string? coverImageAlt,
        string? coverVideoUrl,
        string? authorUserId,
        string? authorDisplayName,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? faqJson)
    {
        ApplyContent(
            title, slug, body, excerpt, categoryId, coverImageUrl, coverImageAlt, coverVideoUrl,
            authorUserId, authorDisplayName,
            metaTitle, metaDescription, seoKeywords, canonicalUrl,
            ogTitle, ogDescription, ogImageUrl, faqJson);
        Touch();
    }

    public void Publish(DateTime? publishedAtUtc = null)
    {
        Status = PostStatus.Published;
        if (publishedAtUtc.HasValue)
            PublishedAtUtc = DateTime.SpecifyKind(publishedAtUtc.Value, DateTimeKind.Utc);
        else
            PublishedAtUtc ??= DateTime.UtcNow;
        Touch();
    }

    public void Unpublish()
    {
        Status = PostStatus.Draft;
        Touch();
    }

    public void SetPublishedAt(DateTime? publishedAtUtc)
    {
        PublishedAtUtc = publishedAtUtc.HasValue
            ? DateTime.SpecifyKind(publishedAtUtc.Value, DateTimeKind.Utc)
            : null;
        Touch();
    }

    public void SetOwnedBy(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("شناسه مالک مطلب الزامی است.");
        if (userId.Trim().Length > 450)
            throw new DomainException("شناسه مالک مطلب نامعتبر است.");

        OwnedByUserId = userId.Trim();
        Touch();
    }

    private void ApplyContent(
        string title,
        string slug,
        string body,
        string? excerpt,
        Guid? categoryId,
        string? coverImageUrl,
        string? coverImageAlt,
        string? coverVideoUrl,
        string? authorUserId,
        string? authorDisplayName,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? faqJson)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("عنوان مطلب الزامی است.");
        if (title.Trim().Length > 300)
            throw new DomainException("عنوان مطلب خیلی طولانی است.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("نامک مطلب الزامی است.");
        if (slug.Trim().Length > 300)
            throw new DomainException("نامک مطلب خیلی طولانی است.");
        if (body is null)
            throw new DomainException("متن مطلب الزامی است.");
        if (excerpt is { Length: > 1000 })
            throw new DomainException("توضیحات کوتاه خیلی طولانی است.");
        if (coverImageUrl is { Length: > 1000 })
            throw new DomainException("آدرس تصویر شاخص خیلی طولانی است.");
        if (coverImageAlt is { Length: > 300 })
            throw new DomainException("متن جایگزین تصویر شاخص خیلی طولانی است.");
        if (coverVideoUrl is { Length: > 1000 })
            throw new DomainException("آدرس ویدیوی شاخص خیلی طولانی است.");
        if (authorUserId is { Length: > 450 })
            throw new DomainException("شناسه نویسنده نامعتبر است.");
        if (authorDisplayName is { Length: > 200 })
            throw new DomainException("نام نویسنده خیلی طولانی است.");
        if (metaTitle is { Length: > 200 })
            throw new DomainException("عنوان متا خیلی طولانی است.");
        if (metaDescription is { Length: > 500 })
            throw new DomainException("توضیحات متا خیلی طولانی است.");
        if (seoKeywords is { Length: > 500 })
            throw new DomainException("کلمات کلیدی سئو خیلی طولانی است.");
        if (canonicalUrl is { Length: > 1000 })
            throw new DomainException("آدرس کنونیکال خیلی طولانی است.");
        if (ogTitle is { Length: > 200 })
            throw new DomainException("عنوان Open Graph خیلی طولانی است.");
        if (ogDescription is { Length: > 500 })
            throw new DomainException("توضیحات Open Graph خیلی طولانی است.");
        if (ogImageUrl is { Length: > 1000 })
            throw new DomainException("آدرس تصویر Open Graph خیلی طولانی است.");
        if (faqJson is { Length: > 100_000 })
            throw new DomainException("سوالات متداول خیلی طولانی است.");

        Title = title.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Body = body;
        Excerpt = NullIfWhiteSpace(excerpt);
        CategoryId = categoryId;
        CoverImageUrl = NullIfWhiteSpace(coverImageUrl);
        CoverImageAlt = NullIfWhiteSpace(coverImageAlt);
        CoverVideoUrl = NullIfWhiteSpace(coverVideoUrl);
        AuthorUserId = NullIfWhiteSpace(authorUserId);
        AuthorDisplayName = NullIfWhiteSpace(authorDisplayName);
        MetaTitle = NullIfWhiteSpace(metaTitle);
        MetaDescription = NullIfWhiteSpace(metaDescription);
        SeoKeywords = NullIfWhiteSpace(seoKeywords);
        CanonicalUrl = NullIfWhiteSpace(canonicalUrl);
        OgTitle = NullIfWhiteSpace(ogTitle);
        OgDescription = NullIfWhiteSpace(ogDescription);
        OgImageUrl = NullIfWhiteSpace(ogImageUrl);
        FaqJson = NullIfWhiteSpace(faqJson);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.News.Domain.Enums;

namespace CMS.Modules.News.Domain.Entities;

public class Article : BaseEntity
{
    private Article()
    {
    }

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Excerpt { get; private set; }
    public ArticleStatus Status { get; private set; } = ArticleStatus.Draft;
    public ArticleKind Kind { get; private set; } = ArticleKind.News;
    public string? CoverImageUrl { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public DateTime? EventStartAtUtc { get; private set; }
    public DateTime? EventEndAtUtc { get; private set; }
    public string? Location { get; private set; }
    public string? AuthorUserId { get; private set; }
    public string? AuthorDisplayName { get; private set; }
    /// <summary>Identity user who owns this article for per-user auto-drafts.</summary>
    public string? OwnedByUserId { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? SeoKeywords { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public string? OgTitle { get; private set; }
    public string? OgDescription { get; private set; }
    public string? OgImageUrl { get; private set; }

    public static Article Create(
        string title,
        string slug,
        string body,
        string? excerpt,
        ArticleKind kind,
        Guid? categoryId,
        string? coverImageUrl,
        string? authorUserId,
        string? authorDisplayName,
        DateTime? eventStartAtUtc,
        DateTime? eventEndAtUtc,
        string? location,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl)
    {
        var article = new Article();
        article.ApplyContent(
            title, slug, body, excerpt, kind, categoryId, coverImageUrl,
            authorUserId, authorDisplayName,
            eventStartAtUtc, eventEndAtUtc, location,
            metaTitle, metaDescription, seoKeywords, canonicalUrl,
            ogTitle, ogDescription, ogImageUrl);
        return article;
    }

    public void Update(
        string title,
        string slug,
        string body,
        string? excerpt,
        ArticleKind kind,
        Guid? categoryId,
        string? coverImageUrl,
        string? authorUserId,
        string? authorDisplayName,
        DateTime? eventStartAtUtc,
        DateTime? eventEndAtUtc,
        string? location,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl)
    {
        ApplyContent(
            title, slug, body, excerpt, kind, categoryId, coverImageUrl,
            authorUserId, authorDisplayName,
            eventStartAtUtc, eventEndAtUtc, location,
            metaTitle, metaDescription, seoKeywords, canonicalUrl,
            ogTitle, ogDescription, ogImageUrl);
        Touch();
    }

    public void Publish(DateTime? publishedAtUtc = null)
    {
        Status = ArticleStatus.Published;
        if (publishedAtUtc.HasValue)
            PublishedAtUtc = DateTime.SpecifyKind(publishedAtUtc.Value, DateTimeKind.Utc);
        else
            PublishedAtUtc ??= DateTime.UtcNow;
        Touch();
    }

    public void Unpublish()
    {
        Status = ArticleStatus.Draft;
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
        ArticleKind kind,
        Guid? categoryId,
        string? coverImageUrl,
        string? authorUserId,
        string? authorDisplayName,
        DateTime? eventStartAtUtc,
        DateTime? eventEndAtUtc,
        string? location,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl)
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
        if (location is { Length: > 300 })
            throw new DomainException("محل برگزاری خیلی طولانی است.");
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

        if (eventStartAtUtc.HasValue && eventEndAtUtc.HasValue
            && eventEndAtUtc.Value < eventStartAtUtc.Value)
        {
            throw new DomainException("پایان رویداد نمی‌تواند قبل از شروع باشد.");
        }

        Title = title.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Body = body;
        Excerpt = NullIfWhiteSpace(excerpt);
        Kind = kind;
        CategoryId = categoryId;
        CoverImageUrl = NullIfWhiteSpace(coverImageUrl);
        EventStartAtUtc = ToUtc(eventStartAtUtc);
        EventEndAtUtc = ToUtc(eventEndAtUtc);
        Location = kind == ArticleKind.Event ? NullIfWhiteSpace(location) : null;
        if (kind != ArticleKind.Event)
        {
            EventStartAtUtc = null;
            EventEndAtUtc = null;
        }

        AuthorUserId = NullIfWhiteSpace(authorUserId);
        AuthorDisplayName = NullIfWhiteSpace(authorDisplayName);
        MetaTitle = NullIfWhiteSpace(metaTitle);
        MetaDescription = NullIfWhiteSpace(metaDescription);
        SeoKeywords = NullIfWhiteSpace(seoKeywords);
        CanonicalUrl = NullIfWhiteSpace(canonicalUrl);
        OgTitle = NullIfWhiteSpace(ogTitle);
        OgDescription = NullIfWhiteSpace(ogDescription);
        OgImageUrl = NullIfWhiteSpace(ogImageUrl);
    }

    private static DateTime? ToUtc(DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

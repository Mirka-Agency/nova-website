using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Services.Domain.Enums;

namespace CMS.Modules.Services.Domain.Entities;

public class ServiceItem : BaseEntity
{
    private ServiceItem()
    {
    }

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Excerpt { get; private set; }
    public ServiceStatus Status { get; private set; } = ServiceStatus.Draft;
    public string? CoverImageUrl { get; private set; }
    public string? IconUrl { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public string? AuthorUserId { get; private set; }
    public string? AuthorDisplayName { get; private set; }
    /// <summary>Identity user who owns this item for per-user auto-drafts.</summary>
    public string? OwnedByUserId { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? SeoKeywords { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public string? OgTitle { get; private set; }
    public string? OgDescription { get; private set; }
    public string? OgImageUrl { get; private set; }

    public static ServiceItem Create(
        string title,
        string slug,
        string body,
        string? excerpt,
        Guid? categoryId,
        string? coverImageUrl,
        string? iconUrl,
        string? authorUserId,
        string? authorDisplayName,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl)
    {
        var item = new ServiceItem();
        item.ApplyContent(
            title, slug, body, excerpt, categoryId, coverImageUrl, iconUrl,
            authorUserId, authorDisplayName,
            metaTitle, metaDescription, seoKeywords, canonicalUrl,
            ogTitle, ogDescription, ogImageUrl);
        return item;
    }

    public void Update(
        string title,
        string slug,
        string body,
        string? excerpt,
        Guid? categoryId,
        string? coverImageUrl,
        string? iconUrl,
        string? authorUserId,
        string? authorDisplayName,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl)
    {
        ApplyContent(
            title, slug, body, excerpt, categoryId, coverImageUrl, iconUrl,
            authorUserId, authorDisplayName,
            metaTitle, metaDescription, seoKeywords, canonicalUrl,
            ogTitle, ogDescription, ogImageUrl);
        Touch();
    }

    public void Publish(DateTime? publishedAtUtc = null)
    {
        Status = ServiceStatus.Published;
        if (publishedAtUtc.HasValue)
            PublishedAtUtc = DateTime.SpecifyKind(publishedAtUtc.Value, DateTimeKind.Utc);
        else
            PublishedAtUtc ??= DateTime.UtcNow;
        Touch();
    }

    public void Unpublish()
    {
        Status = ServiceStatus.Draft;
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
            throw new DomainException("شناسه مالک خدمت الزامی است.");
        if (userId.Trim().Length > 450)
            throw new DomainException("شناسه مالک خدمت نامعتبر است.");

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
        string? iconUrl,
        string? authorUserId,
        string? authorDisplayName,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("عنوان خدمت الزامی است.");
        if (title.Trim().Length > 300)
            throw new DomainException("عنوان خدمت خیلی طولانی است.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("نامک خدمت الزامی است.");
        if (slug.Trim().Length > 300)
            throw new DomainException("نامک خدمت خیلی طولانی است.");
        if (body is null)
            throw new DomainException("متن خدمت الزامی است.");
        if (excerpt is { Length: > 1000 })
            throw new DomainException("توضیحات کوتاه خیلی طولانی است.");
        if (coverImageUrl is { Length: > 1000 })
            throw new DomainException("آدرس تصویر شاخص خیلی طولانی است.");
        if (iconUrl is { Length: > 1000 })
            throw new DomainException("آدرس آیکون خیلی طولانی است.");
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

        Title = title.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Body = body;
        Excerpt = NullIfWhiteSpace(excerpt);
        CategoryId = categoryId;
        CoverImageUrl = NullIfWhiteSpace(coverImageUrl);
        IconUrl = NullIfWhiteSpace(iconUrl);
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

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Team.Domain.Enums;

namespace CMS.Modules.Team.Domain.Entities;

public class TeamItem : BaseEntity
{
    private TeamItem()
    {
    }

    public string Title { get; private set; } = string.Empty;
    public string? Subtitle { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Excerpt { get; private set; }
    /// <summary>Newline-separated credential/highlight lines for doctor-meta.</summary>
    public string? Highlights { get; private set; }
    /// <summary>JSON array of activity-area items (title + text) for doctor-focus.</summary>
    public string? SpecialtyPathJson { get; private set; }
    /// <summary>JSON array of education timeline items (year + title + place) for doctor-education.</summary>
    public string? EducationPathJson { get; private set; }
    /// <summary>JSON array of FAQ items (question + answer) for doctor-faq.</summary>
    public string? FaqJson { get; private set; }
    /// <summary>JSON array of scientific activity items (text) for doctor-activity.</summary>
    public string? ScientificActivityJson { get; private set; }
    public TeamStatus Status { get; private set; } = TeamStatus.Draft;
    public string? CoverImageUrl { get; private set; }
    public string? AvatarImageUrl { get; private set; }
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

    public static TeamItem Create(
        string title,
        string? subtitle,
        string slug,
        string body,
        string? excerpt,
        string? highlights,
        string? specialtyPathJson,
        string? educationPathJson,
        string? faqJson,
        string? scientificActivityJson,
        Guid? categoryId,
        string? coverImageUrl,
        string? avatarImageUrl,
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
        var item = new TeamItem();
        item.ApplyContent(
            title, subtitle, slug, body, excerpt, highlights, specialtyPathJson, educationPathJson, faqJson,
            scientificActivityJson,
            categoryId, coverImageUrl, avatarImageUrl,
            authorUserId, authorDisplayName,
            metaTitle, metaDescription, seoKeywords, canonicalUrl,
            ogTitle, ogDescription, ogImageUrl);
        return item;
    }

    public void Update(
        string title,
        string? subtitle,
        string slug,
        string body,
        string? excerpt,
        string? highlights,
        string? specialtyPathJson,
        string? educationPathJson,
        string? faqJson,
        string? scientificActivityJson,
        Guid? categoryId,
        string? coverImageUrl,
        string? avatarImageUrl,
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
            title, subtitle, slug, body, excerpt, highlights, specialtyPathJson, educationPathJson, faqJson,
            scientificActivityJson,
            categoryId, coverImageUrl, avatarImageUrl,
            authorUserId, authorDisplayName,
            metaTitle, metaDescription, seoKeywords, canonicalUrl,
            ogTitle, ogDescription, ogImageUrl);
        Touch();
    }

    public void Publish(DateTime? publishedAtUtc = null)
    {
        Status = TeamStatus.Published;
        if (publishedAtUtc.HasValue)
            PublishedAtUtc = DateTime.SpecifyKind(publishedAtUtc.Value, DateTimeKind.Utc);
        else
            PublishedAtUtc ??= DateTime.UtcNow;
        Touch();
    }

    public void Unpublish()
    {
        Status = TeamStatus.Draft;
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
            throw new DomainException("شناسه مالک تیم الزامی است.");
        if (userId.Trim().Length > 450)
            throw new DomainException("شناسه مالک تیم نامعتبر است.");

        OwnedByUserId = userId.Trim();
        Touch();
    }

    private void ApplyContent(
        string title,
        string? subtitle,
        string slug,
        string body,
        string? excerpt,
        string? highlights,
        string? specialtyPathJson,
        string? educationPathJson,
        string? faqJson,
        string? scientificActivityJson,
        Guid? categoryId,
        string? coverImageUrl,
        string? avatarImageUrl,
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
            throw new DomainException("عنوان تیم الزامی است.");
        if (title.Trim().Length > 300)
            throw new DomainException("عنوان تیم خیلی طولانی است.");
        if (subtitle is { Length: > 300 })
            throw new DomainException("عنوان فرعی خیلی طولانی است.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("نامک تیم الزامی است.");
        if (slug.Trim().Length > 300)
            throw new DomainException("نامک تیم خیلی طولانی است.");
        if (body is null)
            throw new DomainException("توضیحات تیم الزامی است.");
        if (excerpt is { Length: > 1000 })
            throw new DomainException("توضیحات کوتاه خیلی طولانی است.");
        if (highlights is { Length: > 2000 })
            throw new DomainException("نکات برجسته خیلی طولانی است.");
        if (specialtyPathJson is { Length: > 50_000 })
            throw new DomainException("حوزه فعالیت خیلی طولانی است.");
        if (educationPathJson is { Length: > 50_000 })
            throw new DomainException("مسیر تخصصی خیلی طولانی است.");
        if (faqJson is { Length: > 100_000 })
            throw new DomainException("سوالات متداول خیلی طولانی است.");
        if (scientificActivityJson is { Length: > 50_000 })
            throw new DomainException("فعالیت علمی خیلی طولانی است.");
        if (coverImageUrl is { Length: > 1000 })
            throw new DomainException("آدرس تصویر شاخص خیلی طولانی است.");
        if (avatarImageUrl is { Length: > 1000 })
            throw new DomainException("آدرس تصویر آواتار خیلی طولانی است.");
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
        Subtitle = NullIfWhiteSpace(subtitle);
        Slug = slug.Trim().ToLowerInvariant();
        Body = body;
        Excerpt = NullIfWhiteSpace(excerpt);
        Highlights = NullIfWhiteSpace(highlights);
        SpecialtyPathJson = NullIfWhiteSpace(specialtyPathJson);
        EducationPathJson = NullIfWhiteSpace(educationPathJson);
        FaqJson = NullIfWhiteSpace(faqJson);
        ScientificActivityJson = NullIfWhiteSpace(scientificActivityJson);
        CategoryId = categoryId;
        CoverImageUrl = NullIfWhiteSpace(coverImageUrl);
        AvatarImageUrl = NullIfWhiteSpace(avatarImageUrl);
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

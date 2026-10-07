using CMS.Modules.News.Domain.Enums;

namespace CMS.Modules.News.Application.Articles;

public sealed record PublicArticleCategoryDto(string Name, string Slug);

public sealed record PublicArticleSummaryDto(
    string Title,
    string Slug,
    ArticleKind Kind,
    string? CategoryName,
    string? CategorySlug,
    string? CoverImageUrl,
    string? CoverImageAlt,
    DateTime PublishedAtUtc,
    DateTime? EventStartAtUtc,
    DateTime? EventEndAtUtc,
    string? Location,
    string Excerpt,
    string? AuthorDisplayName);

public sealed record PublicArticleDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    ArticleKind Kind,
    string? CategoryName,
    string? CategorySlug,
    string? CoverImageUrl,
    string? CoverImageAlt,
    string? GalleryJson,
    string? AttachmentUrl,
    string? AttachmentFileName,
    DateTime PublishedAtUtc,
    DateTime? EventStartAtUtc,
    DateTime? EventEndAtUtc,
    string? Location,
    string? EventInfoJson,
    string Excerpt,
    string? AuthorDisplayName,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl);

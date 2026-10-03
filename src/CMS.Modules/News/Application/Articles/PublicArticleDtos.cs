using CMS.Modules.News.Domain.Enums;

namespace CMS.Modules.News.Application.Articles;

public sealed record PublicArticleSummaryDto(
    string Title,
    string Slug,
    ArticleKind Kind,
    string? CategoryName,
    string? CoverImageUrl,
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
    string? CoverImageUrl,
    string? GalleryJson,
    DateTime PublishedAtUtc,
    DateTime? EventStartAtUtc,
    DateTime? EventEndAtUtc,
    string? Location,
    string Excerpt,
    string? AuthorDisplayName,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl);

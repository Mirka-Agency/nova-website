namespace CMS.Modules.Video.Application.VideoItems;

public sealed record PublicVideoItemSummaryDto(
    string Title,
    string Slug,
    string? CategoryName,
    string? CoverImageUrl,
    string? VideoUrl,
    DateTime PublishedAtUtc,
    string Excerpt,
    string? AuthorDisplayName);

public sealed record PublicVideoItemDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? CategoryName,
    string? CoverImageUrl,
    string? VideoUrl,
    DateTime PublishedAtUtc,
    string Excerpt,
    string? AuthorDisplayName,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl);

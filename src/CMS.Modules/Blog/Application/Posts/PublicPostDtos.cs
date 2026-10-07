namespace CMS.Modules.Blog.Application.Posts;

public sealed record PublicPostSummaryDto(
    string Title,
    string Slug,
    string? CategoryName,
    string? CoverImageUrl,
    string? CoverImageAlt,
    DateTime PublishedAtUtc,
    string Excerpt,
    string? AuthorDisplayName);

public sealed record PublicPostDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? CategoryName,
    string? CoverImageUrl,
    string? CoverImageAlt,
    string? CoverVideoUrl,
    DateTime PublishedAtUtc,
    string Excerpt,
    string? AuthorDisplayName,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? FaqJson);

namespace CMS.Modules.Services.Application.ServiceItems;

public sealed record PublicServiceItemSummaryDto(
    string Title,
    string Slug,
    string? CategoryName,
    string? CoverImageUrl,
    string? CoverImageAlt,
    DateTime PublishedAtUtc,
    string Excerpt,
    string? AuthorDisplayName);

public sealed record PublicServiceItemDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? CategoryName,
    string? CoverImageUrl,
    string? CoverImageAlt,
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

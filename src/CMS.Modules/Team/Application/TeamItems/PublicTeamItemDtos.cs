namespace CMS.Modules.Team.Application.TeamItems;

public sealed record PublicTeamItemSummaryDto(
    string Title,
    string Slug,
    string? CategoryName,
    string? CoverImageUrl,
    DateTime PublishedAtUtc,
    string Excerpt,
    string? AuthorDisplayName);

public sealed record PublicTeamItemDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? CategoryName,
    string? CoverImageUrl,
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

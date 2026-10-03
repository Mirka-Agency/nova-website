namespace CMS.Modules.Team.Application.TeamItems;

public sealed record PublicTeamItemSummaryDto(
    string Title,
    string? Subtitle,
    string Slug,
    string? CategoryName,
    string? CoverImageUrl,
    string? AvatarImageUrl,
    DateTime PublishedAtUtc,
    string Excerpt,
    string? Highlights,
    string? AuthorDisplayName);

public sealed record PublicTeamItemDetailDto(
    Guid Id,
    string Title,
    string? Subtitle,
    string Slug,
    string Body,
    string? CategoryName,
    string? CoverImageUrl,
    string? AvatarImageUrl,
    DateTime PublishedAtUtc,
    string Excerpt,
    string? Highlights,
    string? SpecialtyPathJson,
    string? EducationPathJson,
    string? AuthorDisplayName,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl);

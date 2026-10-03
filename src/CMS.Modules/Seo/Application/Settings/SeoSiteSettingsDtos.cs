namespace CMS.Modules.Seo.Application.Settings;

public sealed record SeoSiteSettingsDto(
    Guid Id,
    string? OrganizationName,
    string? OrganizationUrl,
    string? OrganizationLogoUrl,
    string? DefaultSchemaType,
    string? RobotsTxtExtra,
    string? TwitterSiteHandle,
    bool EnableBrokenLinkChecks,
    bool SitemapEnabled,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record UpdateSeoSiteSettingsCommand(
    string? OrganizationName,
    string? OrganizationUrl,
    string? OrganizationLogoUrl,
    string? DefaultSchemaType,
    string? RobotsTxtExtra,
    string? TwitterSiteHandle,
    bool EnableBrokenLinkChecks,
    bool SitemapEnabled);

namespace CMS.Application.Settings;

public sealed record SiteSettingsDto(
    string SiteName,
    string? Tagline,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    string? BusinessHours,
    string? FooterText,
    string? LogoUrl,
    string? FaviconUrl,
    string? MetaTitle,
    string? MetaDescription,
    string? DefaultOgImageUrl,
    string? InstagramUrl,
    string? TelegramUrl,
    string? TwitterUrl,
    string? LinkedInUrl,
    string? AparatUrl,
    string? FacebookUrl,
    string? YouTubeUrl,
    string? WhatsAppUrl,
    string? PrivacyHtml,
    string? HeadScripts,
    string? BodyOpenScripts,
    string? BodyCloseScripts,
    bool MaintenanceMode,
    string? MaintenanceMessage);

public sealed record UpdateSiteSettingsCommand(
    string SiteName,
    string? Tagline,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    string? BusinessHours,
    string? FooterText,
    string? LogoUrl,
    string? FaviconUrl,
    string? MetaTitle,
    string? MetaDescription,
    string? DefaultOgImageUrl,
    string? InstagramUrl,
    string? TelegramUrl,
    string? TwitterUrl,
    string? LinkedInUrl,
    string? AparatUrl,
    string? FacebookUrl,
    string? YouTubeUrl,
    string? WhatsAppUrl,
    string? PrivacyHtml,
    bool MaintenanceMode,
    string? MaintenanceMessage);

public sealed record UpdateSiteScriptsCommand(
    string? HeadScripts,
    string? BodyOpenScripts,
    string? BodyCloseScripts);

public interface ISiteSettingsService
{
    Task<SiteSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(UpdateSiteSettingsCommand command, CancellationToken cancellationToken = default);
    Task UpdateScriptsAsync(UpdateSiteScriptsCommand command, CancellationToken cancellationToken = default);
}

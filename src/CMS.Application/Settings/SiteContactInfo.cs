using System.Text.RegularExpressions;

namespace CMS.Application.Settings;

/// <summary>
/// Resolves public contact/brand display values from core SiteSettings only
/// (no hard-coded clinic fallbacks).
/// </summary>
public sealed class SiteContactInfo
{
    public string Brand { get; private init; } = "سایت";
    public string? Tagline { get; private init; }
    public string? LogoUrl { get; private init; }
    public string? FaviconUrl { get; private init; }
    public string? PhoneDisplay { get; private init; }
    public string? PhoneDigits { get; private init; }
    public string? TelHref { get; private init; }
    public string? Email { get; private init; }
    public string? Address { get; private init; }
    public string? FooterText { get; private init; }
    public string? BusinessHours { get; private init; }
    public string? PrivacyHtml { get; private init; }
    public string? InstagramUrl { get; private init; }
    public string? TelegramUrl { get; private init; }
    public string? TwitterUrl { get; private init; }
    public string? LinkedInUrl { get; private init; }
    public string? AparatUrl { get; private init; }
    public string? FacebookUrl { get; private init; }
    public string? YouTubeUrl { get; private init; }
    public string? WhatsAppUrl { get; private init; }

    public bool HasPhone => !string.IsNullOrWhiteSpace(TelHref);
    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
    public bool HasAddress => !string.IsNullOrWhiteSpace(Address);
    public bool HasBusinessHours => !string.IsNullOrWhiteSpace(BusinessHours);
    public bool HasPrivacyHtml => !string.IsNullOrWhiteSpace(PrivacyHtml);
    public bool HasLogo => !string.IsNullOrWhiteSpace(LogoUrl);

    public static SiteContactInfo From(SiteSettingsDto settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var phoneDisplay = NullIfWhite(settings.ContactPhone);
        var phoneDigits = phoneDisplay is null
            ? null
            : Regex.Replace(phoneDisplay, @"[^\d+]", "");
        if (string.IsNullOrWhiteSpace(phoneDigits))
            phoneDigits = null;

        return new SiteContactInfo
        {
            Brand = string.IsNullOrWhiteSpace(settings.SiteName) ? "سایت" : settings.SiteName.Trim(),
            Tagline = NullIfWhite(settings.Tagline),
            LogoUrl = NullIfWhite(settings.LogoUrl),
            FaviconUrl = NullIfWhite(settings.FaviconUrl),
            PhoneDisplay = phoneDisplay,
            PhoneDigits = phoneDigits,
            TelHref = phoneDigits is null ? null : "tel:" + phoneDigits,
            Email = NullIfWhite(settings.ContactEmail),
            Address = NullIfWhite(settings.Address),
            FooterText = NullIfWhite(settings.FooterText),
            BusinessHours = NullIfWhite(settings.BusinessHours),
            PrivacyHtml = NullIfWhite(settings.PrivacyHtml),
            InstagramUrl = NullIfWhite(settings.InstagramUrl),
            TelegramUrl = NullIfWhite(settings.TelegramUrl),
            TwitterUrl = NullIfWhite(settings.TwitterUrl),
            LinkedInUrl = NullIfWhite(settings.LinkedInUrl),
            AparatUrl = NullIfWhite(settings.AparatUrl),
            FacebookUrl = NullIfWhite(settings.FacebookUrl),
            YouTubeUrl = NullIfWhite(settings.YouTubeUrl),
            WhatsAppUrl = NullIfWhite(settings.WhatsAppUrl)
        };
    }

    private static string? NullIfWhite(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

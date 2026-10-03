using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Infrastructure.Settings;

public class SiteSettings : BaseEntity
{
    private SiteSettings()
    {
    }

    public string SiteName { get; private set; } = "میرکا";
    public string? Tagline { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? Address { get; private set; }
    public string? FooterText { get; private set; }

    public string? LogoUrl { get; private set; }
    public string? FaviconUrl { get; private set; }

    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? DefaultOgImageUrl { get; private set; }

    public string? InstagramUrl { get; private set; }
    public string? TelegramUrl { get; private set; }
    public string? TwitterUrl { get; private set; }
    public string? LinkedInUrl { get; private set; }
    public string? AparatUrl { get; private set; }
    public string? FacebookUrl { get; private set; }
    public string? YouTubeUrl { get; private set; }
    public string? WhatsAppUrl { get; private set; }

    public string? HeadScripts { get; private set; }
    public string? BodyOpenScripts { get; private set; }
    public string? BodyCloseScripts { get; private set; }

    public bool MaintenanceMode { get; private set; }
    public string? MaintenanceMessage { get; private set; }

    public static SiteSettings CreateDefault() => new()
    {
        SiteName = "میرکا",
        FooterText = "میرکا"
    };

    public void Update(SiteSettingsValues values)
    {
        if (string.IsNullOrWhiteSpace(values.SiteName))
            throw new DomainException("نام سایت الزامی است.");
        if (values.SiteName.Trim().Length > 200)
            throw new DomainException("نام سایت خیلی طولانی است.");

        SiteName = values.SiteName.Trim();
        Tagline = NormalizeOptional(values.Tagline, 300);
        ContactEmail = NormalizeOptional(values.ContactEmail, 256);
        ContactPhone = NormalizeOptional(values.ContactPhone, 40);
        Address = NormalizeOptional(values.Address, 1000);
        FooterText = NormalizeOptional(values.FooterText, 500);

        LogoUrl = NormalizeOptional(values.LogoUrl, 2000);
        FaviconUrl = NormalizeOptional(values.FaviconUrl, 2000);

        MetaTitle = NormalizeOptional(values.MetaTitle, 200);
        MetaDescription = NormalizeOptional(values.MetaDescription, 500);
        DefaultOgImageUrl = NormalizeOptional(values.DefaultOgImageUrl, 2000);

        InstagramUrl = NormalizeOptional(values.InstagramUrl, 500);
        TelegramUrl = NormalizeOptional(values.TelegramUrl, 500);
        TwitterUrl = NormalizeOptional(values.TwitterUrl, 500);
        LinkedInUrl = NormalizeOptional(values.LinkedInUrl, 500);
        AparatUrl = NormalizeOptional(values.AparatUrl, 500);
        FacebookUrl = NormalizeOptional(values.FacebookUrl, 500);
        YouTubeUrl = NormalizeOptional(values.YouTubeUrl, 500);
        WhatsAppUrl = NormalizeOptional(values.WhatsAppUrl, 500);

        MaintenanceMode = values.MaintenanceMode;
        MaintenanceMessage = NormalizeOptional(values.MaintenanceMessage, 500);
        Touch();
    }

    public void UpdateScripts(string? headScripts, string? bodyOpenScripts, string? bodyCloseScripts)
    {
        HeadScripts = NormalizeOptional(headScripts, 16000);
        BodyOpenScripts = NormalizeOptional(bodyOpenScripts, 16000);
        BodyCloseScripts = NormalizeOptional(bodyCloseScripts, 16000);
        Touch();
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"مقدار نباید بیشتر از {maxLength} نویسه باشد.");
        return trimmed;
    }
}

public sealed record SiteSettingsValues(
    string SiteName,
    string? Tagline,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
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
    bool MaintenanceMode,
    string? MaintenanceMessage);

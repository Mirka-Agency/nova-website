using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Seo.Domain.Enums;

namespace CMS.Modules.Seo.Domain.Entities;

/// <summary>Singleton site-wide SEO settings row.</summary>
public class SeoSiteSettings : BaseEntity
{
    public static readonly Guid SingletonId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    private SeoSiteSettings()
    {
    }

    public string? OrganizationName { get; private set; }
    public string? OrganizationUrl { get; private set; }
    public string? OrganizationLogoUrl { get; private set; }
    public string? DefaultSchemaType { get; private set; }
    public string? RobotsTxtExtra { get; private set; }
    public string? TwitterSiteHandle { get; private set; }
    public bool EnableBrokenLinkChecks { get; private set; }
    public bool SitemapEnabled { get; private set; } = true;

    public static SeoSiteSettings CreateDefault()
    {
        var settings = new SeoSiteSettings { Id = SingletonId };
        settings.Apply(
            organizationName: null,
            organizationUrl: null,
            organizationLogoUrl: null,
            defaultSchemaType: SeoSchemaTypes.Organization,
            robotsTxtExtra: null,
            twitterSiteHandle: null,
            enableBrokenLinkChecks: false,
            sitemapEnabled: true);
        return settings;
    }

    public void Update(
        string? organizationName,
        string? organizationUrl,
        string? organizationLogoUrl,
        string? defaultSchemaType,
        string? robotsTxtExtra,
        string? twitterSiteHandle,
        bool enableBrokenLinkChecks,
        bool sitemapEnabled)
    {
        Apply(
            organizationName,
            organizationUrl,
            organizationLogoUrl,
            defaultSchemaType,
            robotsTxtExtra,
            twitterSiteHandle,
            enableBrokenLinkChecks,
            sitemapEnabled);
        Touch();
    }

    private void Apply(
        string? organizationName,
        string? organizationUrl,
        string? organizationLogoUrl,
        string? defaultSchemaType,
        string? robotsTxtExtra,
        string? twitterSiteHandle,
        bool enableBrokenLinkChecks,
        bool sitemapEnabled)
    {
        Validate(organizationName, organizationUrl, organizationLogoUrl, defaultSchemaType, robotsTxtExtra, twitterSiteHandle);

        OrganizationName = NullIfWhiteSpace(organizationName, 200);
        OrganizationUrl = NullIfWhiteSpace(organizationUrl, 1000);
        OrganizationLogoUrl = NullIfWhiteSpace(organizationLogoUrl, 1000);
        DefaultSchemaType = string.IsNullOrWhiteSpace(defaultSchemaType)
            ? null
            : SeoSchemaTypes.Normalize(defaultSchemaType);
        RobotsTxtExtra = NullIfWhiteSpace(robotsTxtExtra, 8000);
        TwitterSiteHandle = NormalizeTwitterHandle(twitterSiteHandle);
        EnableBrokenLinkChecks = enableBrokenLinkChecks;
        SitemapEnabled = sitemapEnabled;
    }

    private static void Validate(
        string? organizationName,
        string? organizationUrl,
        string? organizationLogoUrl,
        string? defaultSchemaType,
        string? robotsTxtExtra,
        string? twitterSiteHandle)
    {
        if (!string.IsNullOrWhiteSpace(organizationName) && organizationName.Trim().Length > 200)
            throw new DomainException("نام سازمان خیلی طولانی است.");

        if (!string.IsNullOrWhiteSpace(organizationUrl) && organizationUrl.Trim().Length > 1000)
            throw new DomainException("آدرس سازمان خیلی طولانی است.");

        if (!string.IsNullOrWhiteSpace(organizationLogoUrl) && organizationLogoUrl.Trim().Length > 1000)
            throw new DomainException("آدرس لوگو خیلی طولانی است.");

        if (!string.IsNullOrWhiteSpace(defaultSchemaType) && SeoSchemaTypes.Normalize(defaultSchemaType) is null)
            throw new DomainException("نوع اسکیمای پیش‌فرض نامعتبر است.");

        if (!string.IsNullOrWhiteSpace(robotsTxtExtra) && robotsTxtExtra.Length > 8000)
            throw new DomainException("متن اضافی robots.txt خیلی طولانی است.");

        if (!string.IsNullOrWhiteSpace(twitterSiteHandle) && twitterSiteHandle.Trim().Length > 100)
            throw new DomainException("هندل توییتر خیلی طولانی است.");
    }

    private static string? NullIfWhiteSpace(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static string? NormalizeTwitterHandle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var handle = value.Trim();
        if (!handle.StartsWith('@'))
            handle = "@" + handle;

        return handle.Length > 100 ? handle[..100] : handle;
    }
}

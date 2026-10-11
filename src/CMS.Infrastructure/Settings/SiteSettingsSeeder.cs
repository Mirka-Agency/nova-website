using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Infrastructure.Settings;

public static class SiteSettingsSeeder
{
    public const string ClinicAddress =
        "شریعتی بالاتر از خواجه عبدالله نبش کوچه ذکایی(جنب آرش موتورز) ساختمان کسری طبقه ۴ واحد ۱۵";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SiteSettingsSeeder");

        var settings = await db.SiteSettings.OrderBy(s => s.CreatedAtUtc).FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = SiteSettings.CreateDefault();
            db.SiteSettings.Add(settings);
            await db.SaveChangesAsync();
            logger.LogInformation("Default site settings seeded");
        }

        if (ShouldNormalizeAddress(settings.Address))
        {
            settings.Update(new SiteSettingsValues(
                settings.SiteName,
                settings.Tagline,
                settings.ContactEmail,
                settings.ContactPhone,
                ClinicAddress,
                settings.BusinessHours,
                settings.FooterText,
                settings.LogoUrl,
                settings.FaviconUrl,
                settings.MetaTitle,
                settings.MetaDescription,
                settings.DefaultOgImageUrl,
                settings.InstagramUrl,
                settings.TelegramUrl,
                settings.TwitterUrl,
                settings.LinkedInUrl,
                settings.AparatUrl,
                settings.FacebookUrl,
                settings.YouTubeUrl,
                settings.WhatsAppUrl,
                settings.PrivacyHtml,
                settings.MaintenanceMode,
                settings.MaintenanceMessage));
            await db.SaveChangesAsync();
            logger.LogInformation("Site address normalized to clinic Neshan location");
        }
    }

    private static bool ShouldNormalizeAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return true;

        if (string.Equals(address.Trim(), ClinicAddress, StringComparison.Ordinal))
            return false;

        // Older short footer form without آرش موتورز / خواجه عبدالله
        return address.Contains("ذکایی", StringComparison.Ordinal)
               && !address.Contains("آرش موتورز", StringComparison.Ordinal);
    }
}

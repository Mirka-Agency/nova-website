using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Infrastructure.Settings;

public static class SiteSettingsSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SiteSettingsSeeder");

        if (await db.SiteSettings.AnyAsync())
            return;

        db.SiteSettings.Add(SiteSettings.CreateDefault());
        await db.SaveChangesAsync();
        logger.LogInformation("Default site settings seeded");
    }
}

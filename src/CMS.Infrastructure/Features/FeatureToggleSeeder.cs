using CMS.Application.Common.Features;
using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Infrastructure.Features;

public static class FeatureToggleSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("FeatureToggleSeeder");

        var features = new[]
        {
            FeatureNames.Blog,
            FeatureNames.News,
            FeatureNames.Services,
            FeatureNames.Video,
            FeatureNames.Team,
            FeatureNames.Shop,
            FeatureNames.Forms,
            FeatureNames.Comments,
            FeatureNames.Popup,
            FeatureNames.Seo
        };

        foreach (var name in features)
        {
            var exists = await db.FeatureToggles.AnyAsync(t => t.Name == name, cancellationToken);
            if (exists)
            {
                continue;
            }

            var enabled = configuration.GetValue($"FeatureManagement:{name}", false);
            db.FeatureToggles.Add(new FeatureToggle
            {
                Name = name,
                Enabled = enabled,
                UpdatedAtUtc = DateTime.UtcNow
            });
            logger.LogInformation("Seeded feature toggle {Feature} = {Enabled}", name, enabled);
        }

        // Keep Forms reachable when config enables it (system contact/booking forms).
        if (configuration.GetValue($"FeatureManagement:{FeatureNames.Forms}", false))
        {
            var formsToggle = await db.FeatureToggles
                .FirstOrDefaultAsync(t => t.Name == FeatureNames.Forms, cancellationToken);
            if (formsToggle is not null && !formsToggle.Enabled)
            {
                formsToggle.Enabled = true;
                formsToggle.UpdatedAtUtc = DateTime.UtcNow;
                logger.LogInformation("Enabled feature toggle {Feature} from configuration", FeatureNames.Forms);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

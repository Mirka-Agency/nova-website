using CMS.Modules.Honors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Modules.Honors.Infrastructure.Persistence;

public static class HonorDefaultSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HonorsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("HonorDefaultSeeder");

        if (await db.HonorItems.AnyAsync(cancellationToken))
            return;

        var seeds = new (string Title, string ImageUrl, string AltText, int SortOrder)[]
        {
            ("مجوز رسمی DHA",
                "/template/assets/images/about/cert-dha.webp",
                "مجوز رسمی DHA",
                0),
            ("دانشنامه تخصص جراحی",
                "/template/assets/images/about/cert-specialty.webp",
                "دانشنامه تخصص جراحی — دکتر کاوه همدانی",
                1),
            ("فلوشیپ جراحی غدد درون‌ریز",
                "/template/assets/images/about/cert-fellowship.webp",
                "گواهینامه فلوشیپ جراحی غدد درون‌ریز — دکتر کاوه همدانی",
                2),
            ("پسادکتری دانشگاه ییل",
                "/template/assets/images/about/cert-yale.webp",
                "گواهینامه پسادکتری دانشگاه ییل — دکتر میرعلیرضا تکیار",
                3),
            ("گواهینامه حرفه‌ای",
                "/template/assets/images/about/cert-license.jpeg",
                "گواهینامه حرفه‌ای تیم نووا کلینیک",
                4)
        };

        foreach (var seed in seeds)
        {
            db.HonorItems.Add(HonorItem.Create(
                seed.Title,
                seed.ImageUrl,
                seed.AltText,
                seed.SortOrder,
                isPublished: true));
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} default honor items", seeds.Length);
    }
}

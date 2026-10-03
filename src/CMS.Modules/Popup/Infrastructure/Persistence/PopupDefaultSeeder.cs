using CMS.Modules.Popup.Domain.Entities;
using CMS.Modules.Popup.Domain.Enums;
using CMS.Modules.Popup.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Modules.Popup.Infrastructure.Persistence;

public static class PopupDefaultSeeder
{
    public const string DefaultSlug = "default-welcome";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PopupDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("PopupDefaultSeeder");

        if (await db.Popups.AnyAsync(p => p.Slug == DefaultSlug, cancellationToken))
            return;

        var content = new PopupContent(
            Title: "پاپ‌آپ خوش‌آمدگویی",
            Slug: DefaultSlug,
            BodyText: "به سایت ما خوش آمدید. این پاپ‌آپ پیش‌فرض است و می‌توانید آن را از پنل مدیریت ویرایش، فعال یا غیرفعال کنید.",
            ImageUrl: null,
            ContentHtml: null,
            IsActive: true,
            FormId: null,
            CtaText: "متوجه شدم",
            CtaAction: PopupCtaActions.Close,
            CtaUrl: null,
            CtaTargetPopupId: null,
            TriggerType: PopupTriggerTypes.Manual,
            TriggerDelaySeconds: null,
            TriggerScrollPercent: null,
            TriggerSelector: null,
            TriggerConfigJson: null);

        var behavior = new PopupBehavior(
            ShowOverlay: true,
            ShowCloseButton: true,
            CloseOnOverlayClick: true,
            CloseOnEscape: true,
            LockBodyScroll: true,
            EnableContentScroll: true,
            Frequency: PopupFrequency.Always,
            PageTargetMode: PopupPageTargetMode.All,
            PagePaths: null,
            ExtensionSettingsJson: null);

        var popup = PopupItem.Create(content, behavior);
        db.Popups.Add(popup);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded default popup {Slug} ({PopupId})", DefaultSlug, popup.Id);
    }
}

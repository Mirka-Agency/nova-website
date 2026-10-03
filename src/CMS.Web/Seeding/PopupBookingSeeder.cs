using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.Popup.Domain.Entities;
using CMS.Modules.Popup.Domain.Enums;
using CMS.Modules.Popup.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Web.Seeding;

/// <summary>
/// Ensures the sitewide booking dialog is a Core Popup (slug=booking) backed by Forms key=booking.
/// Existing <c>data-booking-open</c> buttons open it via TriggerSelector.
/// </summary>
public static class PopupBookingSeeder
{
    public const string BookingSlug = "booking";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PopupDbContext>();
        var forms = scope.ServiceProvider.GetRequiredService<IFormService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("PopupBookingSeeder");

        var bookingForm = await forms.GetPublicContractByKeyAsync("booking", cancellationToken);
        var existing = await db.Popups.FirstOrDefaultAsync(p => p.Slug == BookingSlug, cancellationToken);

        if (existing is not null)
        {
            if (bookingForm is not null
                && existing.FormId != bookingForm.Id
                && existing.FormId is null)
            {
                existing.Update(
                    new PopupContent(
                        existing.Title,
                        existing.Slug,
                        existing.BodyText,
                        existing.ImageUrl,
                        existing.ContentHtml,
                        existing.IsActive,
                        bookingForm.Id,
                        existing.CtaText,
                        existing.CtaAction,
                        existing.CtaUrl,
                        existing.CtaTargetPopupId,
                        existing.TriggerType,
                        existing.TriggerDelaySeconds,
                        existing.TriggerScrollPercent,
                        existing.TriggerSelector ?? "[data-booking-open]",
                        existing.TriggerConfigJson),
                    new PopupBehavior(
                        existing.ShowOverlay,
                        existing.ShowCloseButton,
                        existing.CloseOnOverlayClick,
                        existing.CloseOnEscape,
                        existing.LockBodyScroll,
                        existing.EnableContentScroll,
                        existing.Frequency,
                        existing.PageTargetMode,
                        existing.PagePaths,
                        existing.ExtensionSettingsJson),
                    existing.SortOrder);
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Linked booking popup {PopupId} to form {FormId}", existing.Id, bookingForm.Id);
            }

            return;
        }

        var content = new PopupContent(
            Title: bookingForm?.Name ?? "رزرو نوبت مشاوره",
            Slug: BookingSlug,
            BodyText: bookingForm?.Description
                ?? "نام و شماره تماس خود را وارد کنید تا برای هماهنگی نوبت با شما تماس بگیریم.",
            ImageUrl: null,
            ContentHtml: null,
            IsActive: true,
            FormId: bookingForm?.Id,
            CtaText: null,
            CtaAction: PopupCtaActions.None,
            CtaUrl: null,
            CtaTargetPopupId: null,
            TriggerType: PopupTriggerTypes.Manual,
            TriggerDelaySeconds: null,
            TriggerScrollPercent: null,
            TriggerSelector: "[data-booking-open]",
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

        var popup = PopupItem.Create(content, behavior, sortOrder: 0);
        db.Popups.Add(popup);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seeded booking popup {Slug} ({PopupId}) form={FormId}",
            BookingSlug,
            popup.Id,
            bookingForm?.Id);
    }
}

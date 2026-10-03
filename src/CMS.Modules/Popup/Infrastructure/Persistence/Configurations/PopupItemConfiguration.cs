using CMS.Modules.Popup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.Popup.Infrastructure.Persistence.Configurations;

public sealed class PopupItemConfiguration : IEntityTypeConfiguration<PopupItem>
{
    public void Configure(EntityTypeBuilder<PopupItem> builder)
    {
        builder.ToTable("Popups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BodyText).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.Property(x => x.ContentHtml).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.FormId);
        builder.Property(x => x.CtaText).HasMaxLength(120).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(x => x.CtaAction).HasMaxLength(32).IsRequired().HasDefaultValue("none");
        builder.Property(x => x.CtaUrl).HasMaxLength(1000);
        builder.Property(x => x.CtaTargetPopupId);
        builder.Property(x => x.TriggerType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TriggerDelaySeconds);
        builder.Property(x => x.TriggerScrollPercent);
        builder.Property(x => x.TriggerSelector).HasMaxLength(300);
        builder.Property(x => x.TriggerConfigJson);
        builder.Property(x => x.ShowOverlay).IsRequired();
        builder.Property(x => x.ShowCloseButton).IsRequired();
        builder.Property(x => x.CloseOnOverlayClick).IsRequired();
        builder.Property(x => x.CloseOnEscape).IsRequired();
        builder.Property(x => x.LockBodyScroll).IsRequired();
        builder.Property(x => x.EnableContentScroll).IsRequired();
        builder.Property(x => x.Frequency)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.PageTargetMode)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.PagePaths).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(x => x.ExtensionSettingsJson);
        builder.Property(x => x.SortOrder).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}

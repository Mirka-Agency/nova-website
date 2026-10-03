using CMS.Modules.Voices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.Voices.Infrastructure.Persistence.Configurations;

public sealed class VoiceItemConfiguration : IEntityTypeConfiguration<VoiceItem>
{
    public void Configure(EntityTypeBuilder<VoiceItem> builder)
    {
        builder.ToTable("VoiceItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Subtitle).HasMaxLength(300);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.AudioUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.IsPublished).IsRequired();
        builder.Property(x => x.PublishedAtUtc);
        builder.HasIndex(x => new { x.IsPublished, x.SortOrder });
    }
}

using CMS.Modules.Media.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.Media.Infrastructure.Persistence.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("Assets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ObjectKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300);
        builder.Property(x => x.AltText).HasMaxLength(500);
        builder.Property(x => x.Caption).HasMaxLength(1000);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.ThumbnailObjectKey).HasMaxLength(500);
        builder.Property(x => x.OriginalFileName).HasMaxLength(260);
        builder.Property(x => x.OriginalContentType).HasMaxLength(128);
        builder.Property(x => x.OriginalObjectKey).HasMaxLength(500);
        builder.Property(x => x.VariantsJson).HasColumnType("jsonb");
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.ObjectKey).IsUnique();
    }
}

using CMS.Modules.Honors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.Honors.Infrastructure.Persistence.Configurations;

public sealed class HonorItemConfiguration : IEntityTypeConfiguration<HonorItem>
{
    public void Configure(EntityTypeBuilder<HonorItem> builder)
    {
        builder.ToTable("HonorItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.AltText).HasMaxLength(300);
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.IsPublished).IsRequired();
        builder.Property(x => x.PublishedAtUtc);
        builder.HasIndex(x => new { x.IsPublished, x.SortOrder });
    }
}

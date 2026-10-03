using CMS.Modules.Seo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.Seo.Infrastructure.Persistence.Configurations;

public sealed class SeoDocumentConfiguration : IEntityTypeConfiguration<SeoDocument>
{
    public void Configure(EntityTypeBuilder<SeoDocument> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContentType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ContentId).IsRequired();
        builder.Property(x => x.FocusKeyword).HasMaxLength(200);
        builder.Property(x => x.RobotsIndex).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.RobotsFollow).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.SchemaType).HasMaxLength(64);
        builder.Property(x => x.SchemaJson);
        builder.Property(x => x.SeoScore);
        builder.Property(x => x.AnalysisJson);
        builder.HasIndex(x => new { x.ContentType, x.ContentId }).IsUnique();
    }
}

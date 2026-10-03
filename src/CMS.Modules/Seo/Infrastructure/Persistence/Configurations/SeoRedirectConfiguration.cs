using CMS.Modules.Seo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.Seo.Infrastructure.Persistence.Configurations;

public sealed class SeoRedirectConfiguration : IEntityTypeConfiguration<SeoRedirect>
{
    public void Configure(EntityTypeBuilder<SeoRedirect> builder)
    {
        builder.ToTable("Redirects");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.FromPath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ToUrl).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.StatusCode).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => x.FromPath).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.FromPath });
    }
}

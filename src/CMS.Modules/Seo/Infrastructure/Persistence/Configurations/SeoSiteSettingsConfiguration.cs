using CMS.Modules.Seo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.Seo.Infrastructure.Persistence.Configurations;

public sealed class SeoSiteSettingsConfiguration : IEntityTypeConfiguration<SeoSiteSettings>
{
    public void Configure(EntityTypeBuilder<SeoSiteSettings> builder)
    {
        builder.ToTable("SiteSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OrganizationName).HasMaxLength(200);
        builder.Property(x => x.OrganizationUrl).HasMaxLength(1000);
        builder.Property(x => x.OrganizationLogoUrl).HasMaxLength(1000);
        builder.Property(x => x.DefaultSchemaType).HasMaxLength(64);
        builder.Property(x => x.RobotsTxtExtra);
        builder.Property(x => x.TwitterSiteHandle).HasMaxLength(100);
        builder.Property(x => x.EnableBrokenLinkChecks).IsRequired();
        builder.Property(x => x.SitemapEnabled).IsRequired().HasDefaultValue(true);
    }
}

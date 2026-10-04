using CMS.Modules.News.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.News.Infrastructure.Persistence.Configurations;

public sealed class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("Articles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Body).IsRequired();
        builder.Property(x => x.Excerpt).HasMaxLength(1000);
        builder.Property(x => x.CoverImageUrl).HasMaxLength(1000);
        builder.Property(x => x.GalleryJson);
        builder.Property(x => x.EventInfoJson);
        builder.Property(x => x.AttachmentUrl).HasMaxLength(1000);
        builder.Property(x => x.AttachmentFileName).HasMaxLength(300);
        builder.Property(x => x.Location).HasMaxLength(300);
        builder.Property(x => x.AuthorUserId).HasMaxLength(450);
        builder.Property(x => x.AuthorDisplayName).HasMaxLength(200);
        builder.Property(x => x.OwnedByUserId).HasMaxLength(450);
        builder.Property(x => x.MetaTitle).HasMaxLength(200);
        builder.Property(x => x.MetaDescription).HasMaxLength(500);
        builder.Property(x => x.SeoKeywords).HasMaxLength(500);
        builder.Property(x => x.CanonicalUrl).HasMaxLength(1000);
        builder.Property(x => x.OgTitle).HasMaxLength(200);
        builder.Property(x => x.OgDescription).HasMaxLength(500);
        builder.Property(x => x.OgImageUrl).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.OwnedByUserId, x.Status });
        builder.HasIndex(x => new { x.Status, x.PublishedAtUtc });
        builder.HasIndex(x => x.Kind);
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Articles)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

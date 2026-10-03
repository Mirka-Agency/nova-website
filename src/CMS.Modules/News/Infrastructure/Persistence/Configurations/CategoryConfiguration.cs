using CMS.Modules.News.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.News.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Navigation(x => x.Articles).HasField("_articles");
        builder.Metadata.FindNavigation(nameof(Category.Articles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

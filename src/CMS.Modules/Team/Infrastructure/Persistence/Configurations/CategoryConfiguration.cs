using CMS.Modules.Team.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.Team.Infrastructure.Persistence.Configurations;

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
        builder.Navigation(x => x.TeamItems).HasField("_teamItems");
        builder.Metadata.FindNavigation(nameof(Category.TeamItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

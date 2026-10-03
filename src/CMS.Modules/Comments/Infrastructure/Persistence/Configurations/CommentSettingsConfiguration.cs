using CMS.Modules.Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.Comments.Infrastructure.Persistence.Configurations;

public sealed class CommentSettingsConfiguration : IEntityTypeConfiguration<CommentSettings>
{
    public void Configure(EntityTypeBuilder<CommentSettings> builder)
    {
        builder.ToTable("Settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CaptchaProvider).HasConversion<int>().IsRequired();
    }
}

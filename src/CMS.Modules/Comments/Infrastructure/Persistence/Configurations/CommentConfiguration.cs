using CMS.Modules.Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.Comments.Infrastructure.Persistence.Configurations;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TargetType).HasConversion<int>().IsRequired();
        builder.Property(x => x.TargetTitle).HasMaxLength(300).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.AuthorName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AuthorEmail).HasMaxLength(256);
        builder.Property(x => x.AuthorPhone).HasMaxLength(40);
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.HasIndex(x => new { x.TargetType, x.TargetId, x.Status });
        builder.HasIndex(x => x.PublishedAtUtc);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}

using CMS.Modules.Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Comments.Infrastructure.Persistence;

public class CommentsDbContext : DbContext
{
    public CommentsDbContext(DbContextOptions<CommentsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<CommentSettings> Settings => Set<CommentSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("comments");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommentsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

using CMS.Modules.News.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.News.Infrastructure.Persistence;

public class NewsDbContext : DbContext
{
    public NewsDbContext(DbContextOptions<NewsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("news");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NewsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

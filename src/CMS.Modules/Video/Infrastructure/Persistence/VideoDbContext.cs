using CMS.Modules.Video.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Video.Infrastructure.Persistence;

public class VideoDbContext : DbContext
{
    public VideoDbContext(DbContextOptions<VideoDbContext> options)
        : base(options)
    {
    }

    public DbSet<VideoItem> VideoItems => Set<VideoItem>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("video");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VideoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

using CMS.Modules.Media.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Media.Infrastructure.Persistence;

public class MediaDbContext : DbContext
{
    public MediaDbContext(DbContextOptions<MediaDbContext> options)
        : base(options)
    {
    }

    public DbSet<MediaAsset> Assets => Set<MediaAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("media");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

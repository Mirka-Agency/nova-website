using CMS.Modules.Honors.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Honors.Infrastructure.Persistence;

public class HonorsDbContext : DbContext
{
    public HonorsDbContext(DbContextOptions<HonorsDbContext> options)
        : base(options)
    {
    }

    public DbSet<HonorItem> HonorItems => Set<HonorItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("honors");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HonorsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

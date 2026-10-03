using CMS.Modules.Services.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Services.Infrastructure.Persistence;

public class ServicesDbContext : DbContext
{
    public ServicesDbContext(DbContextOptions<ServicesDbContext> options)
        : base(options)
    {
    }

    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("services");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServicesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

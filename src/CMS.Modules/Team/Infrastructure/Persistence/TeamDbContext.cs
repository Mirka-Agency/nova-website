using CMS.Modules.Team.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Team.Infrastructure.Persistence;

public class TeamDbContext : DbContext
{
    public TeamDbContext(DbContextOptions<TeamDbContext> options)
        : base(options)
    {
    }

    public DbSet<TeamItem> TeamItems => Set<TeamItem>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("team");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeamDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

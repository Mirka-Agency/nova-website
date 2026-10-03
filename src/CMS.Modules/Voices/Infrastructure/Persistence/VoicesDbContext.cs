using CMS.Modules.Voices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Voices.Infrastructure.Persistence;

public class VoicesDbContext : DbContext
{
    public VoicesDbContext(DbContextOptions<VoicesDbContext> options)
        : base(options)
    {
    }

    public DbSet<VoiceItem> VoiceItems => Set<VoiceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("voices");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VoicesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

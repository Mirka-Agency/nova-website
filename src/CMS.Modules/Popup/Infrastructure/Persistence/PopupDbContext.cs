using CMS.Modules.Popup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Popup.Infrastructure.Persistence;

public class PopupDbContext : DbContext
{
    public PopupDbContext(DbContextOptions<PopupDbContext> options)
        : base(options)
    {
    }

    public DbSet<PopupItem> Popups => Set<PopupItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("popup");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PopupDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

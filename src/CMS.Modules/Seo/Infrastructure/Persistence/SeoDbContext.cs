using CMS.Modules.Seo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Seo.Infrastructure.Persistence;

public class SeoDbContext : DbContext
{
    public SeoDbContext(DbContextOptions<SeoDbContext> options) : base(options)
    {
    }

    public DbSet<SeoDocument> Documents => Set<SeoDocument>();
    public DbSet<SeoRedirect> Redirects => Set<SeoRedirect>();
    public DbSet<SeoSiteSettings> SiteSettings => Set<SeoSiteSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("seo");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SeoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

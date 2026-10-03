using CMS.Modules.Forms.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Forms.Infrastructure.Persistence;

public class FormsDbContext : DbContext
{
    public FormsDbContext(DbContextOptions<FormsDbContext> options)
        : base(options)
    {
    }

    public DbSet<FormDefinition> Forms => Set<FormDefinition>();
    public DbSet<FormField> Fields => Set<FormField>();
    public DbSet<FormVersion> FormVersions => Set<FormVersion>();
    public DbSet<FormSubmission> Submissions => Set<FormSubmission>();
    public DbSet<FormSubmissionFile> SubmissionFiles => Set<FormSubmissionFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("forms");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FormsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

using CMS.Modules.Forms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.Forms.Infrastructure.Persistence.Configurations;

public sealed class FormDefinitionConfiguration : IEntityTypeConfiguration<FormDefinition>
{
    public void Configure(EntityTypeBuilder<FormDefinition> builder)
    {
        builder.ToTable("Forms");
        builder.HasKey(x => x.Id);
        // Ids are always client-generated in BaseEntity; ValueGeneratedOnAdd makes EF
        // treat navigation-discovered entities as existing → UPDATE 0 rows (concurrency).
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();
        builder.Ignore(x => x.IsPublished);
        builder.Ignore(x => x.IsEnabled);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.Status);
        // Pointers are soft references (avoid circular FK with FormVersions).
        builder.Property(x => x.PublishedVersionId);
        builder.Property(x => x.DraftVersionId);
        builder.Property(x => x.IsSystem).IsRequired().HasDefaultValue(false);
        builder.HasIndex(x => x.PublishedVersionId);
        builder.HasIndex(x => x.DraftVersionId);
        builder.HasIndex(x => x.IsSystem);

        builder.HasMany(x => x.Fields)
            .WithOne(x => x.Form)
            .HasForeignKey(x => x.FormId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Submissions)
            .WithOne(x => x.Form)
            .HasForeignKey(x => x.FormId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Versions)
            .WithOne(x => x.Form)
            .HasForeignKey(x => x.FormId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Fields).HasField("_fields");
        builder.Metadata.FindNavigation(nameof(FormDefinition.Fields))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.Submissions).HasField("_submissions");
        builder.Metadata.FindNavigation(nameof(FormDefinition.Submissions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.Versions).HasField("_versions");
        builder.Metadata.FindNavigation(nameof(FormDefinition.Versions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class FormVersionConfiguration : IEntityTypeConfiguration<FormVersion>
{
    public void Configure(EntityTypeBuilder<FormVersion> builder)
    {
        builder.ToTable("FormVersions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.VersionNumber).IsRequired();
        builder.Property(x => x.State).HasConversion<int>().IsRequired();
        builder.Property(x => x.SchemaJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.FormId, x.VersionNumber }).IsUnique();
        builder.HasIndex(x => x.FormId);
        builder.HasIndex(x => x.State);
    }
}

public sealed class FormFieldConfiguration : IEntityTypeConfiguration<FormField>
{
    public void Configure(EntityTypeBuilder<FormField> builder)
    {
        builder.ToTable("Fields");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Placeholder).HasMaxLength(300);
        builder.Property(x => x.HelpText).HasMaxLength(1000);
        builder.Property(x => x.FieldType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.OptionsCsv).HasMaxLength(2000);
        builder.Property(x => x.SettingsJson).HasMaxLength(4000);
        builder.Property(x => x.IsSystem).IsRequired().HasDefaultValue(false);
        builder.HasIndex(x => new { x.FormId, x.Key }).IsUnique();
        builder.HasIndex(x => x.FormId);
    }
}

public sealed class FormSubmissionConfiguration : IEntityTypeConfiguration<FormSubmission>
{
    public void Configure(EntityTypeBuilder<FormSubmission> builder)
    {
        builder.ToTable("Submissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.DataJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ContextJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.SubmittedAtUtc);
        builder.HasIndex(x => x.FormId);
        builder.HasIndex(x => x.FormVersionId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.FormVersion)
            .WithMany()
            .HasForeignKey(x => x.FormVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Files)
            .WithOne(x => x.Submission)
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Files).HasField("_files");
        builder.Metadata.FindNavigation(nameof(FormSubmission.Files))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class FormSubmissionFileConfiguration : IEntityTypeConfiguration<FormSubmissionFile>
{
    public void Configure(EntityTypeBuilder<FormSubmissionFile> builder)
    {
        builder.ToTable("SubmissionFiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.FieldKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PublicUrl).HasMaxLength(2000);
        builder.HasIndex(x => x.SubmissionId);
        builder.HasIndex(x => x.FieldId);
        builder.HasIndex(x => x.FieldKey);
    }
}

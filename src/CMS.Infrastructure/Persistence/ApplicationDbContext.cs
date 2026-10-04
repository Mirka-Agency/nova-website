using CMS.Infrastructure.Audit;
using CMS.Infrastructure.Features;
using CMS.Infrastructure.Identity;
using CMS.Infrastructure.Messaging;
using CMS.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

        public DbSet<FeatureToggle> FeatureToggles => Set<FeatureToggle>();
        public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
        public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
        public DbSet<MessageLog> MessageLogs => Set<MessageLog>();
        public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.HasDefaultSchema("identity");

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(x => x.FullName).HasMaxLength(200);
                entity.Property(x => x.AvatarUrl).HasMaxLength(2000);
            });

            builder.Entity<OtpChallenge>(entity =>
            {
                entity.ToTable("OtpChallenges", "core");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Phone).HasMaxLength(20).IsRequired();
                entity.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
                entity.HasIndex(x => x.Phone).IsUnique();
                entity.HasIndex(x => x.ExpiresAtUtc);
            });

            builder.Entity<FeatureToggle>(entity =>
        {
            entity.ToTable("FeatureToggles", "core");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<SiteSettings>(entity =>
        {
            entity.ToTable("SiteSettings", "core");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SiteName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Tagline).HasMaxLength(300);
            entity.Property(x => x.ContactEmail).HasMaxLength(256);
            entity.Property(x => x.ContactPhone).HasMaxLength(40);
            entity.Property(x => x.Address).HasMaxLength(1000);
            entity.Property(x => x.BusinessHours).HasMaxLength(200);
            entity.Property(x => x.FooterText).HasMaxLength(500);
            entity.Property(x => x.LogoUrl).HasMaxLength(2000);
            entity.Property(x => x.FaviconUrl).HasMaxLength(2000);
            entity.Property(x => x.PrivacyHtml);
            entity.Property(x => x.MetaTitle).HasMaxLength(200);
            entity.Property(x => x.MetaDescription).HasMaxLength(500);
            entity.Property(x => x.DefaultOgImageUrl).HasMaxLength(2000);
            entity.Property(x => x.InstagramUrl).HasMaxLength(500);
            entity.Property(x => x.TelegramUrl).HasMaxLength(500);
            entity.Property(x => x.TwitterUrl).HasMaxLength(500);
            entity.Property(x => x.LinkedInUrl).HasMaxLength(500);
            entity.Property(x => x.AparatUrl).HasMaxLength(500);
            entity.Property(x => x.FacebookUrl).HasMaxLength(500);
            entity.Property(x => x.YouTubeUrl).HasMaxLength(500);
            entity.Property(x => x.WhatsAppUrl).HasMaxLength(500);
            entity.Property(x => x.HeadScripts).HasMaxLength(16000);
            entity.Property(x => x.BodyOpenScripts).HasMaxLength(16000);
            entity.Property(x => x.BodyCloseScripts).HasMaxLength(16000);
            entity.Property(x => x.MaintenanceMessage).HasMaxLength(500);
        });

        builder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("AuditEntries", "core");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(100);
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.Property(x => x.UserEmail).HasMaxLength(256);
            entity.Property(x => x.Details).HasMaxLength(2000);
            entity.HasIndex(x => x.CreatedAtUtc);
        });

        builder.Entity<MessageLog>(entity =>
        {
            entity.ToTable("MessageLogs", "core");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Channel).HasConversion<byte>().IsRequired();
            entity.Property(x => x.Status).HasConversion<byte>().IsRequired();
            entity.Property(x => x.Recipient).HasMaxLength(500).IsRequired();
            entity.Property(x => x.RecipientCount).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(300);
            entity.Property(x => x.BodyPreview).HasMaxLength(500);
            entity.Property(x => x.Provider).HasMaxLength(50);
            entity.Property(x => x.ProviderMessageId).HasMaxLength(200);
            entity.Property(x => x.ErrorMessage).HasMaxLength(1000);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => new { x.Channel, x.CreatedAtUtc });
        });
    }
}

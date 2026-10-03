using System;
using CMS.Modules.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CMS.Modules.News.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NewsDbContext))]
partial class NewsDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasDefaultSchema("news")
            .HasAnnotation("ProductVersion", "10.0.10")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("CMS.Modules.News.Domain.Entities.Article", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<string>("AttachmentFileName")
                    .HasMaxLength(300)
                    .HasColumnType("character varying(300)");

                b.Property<string>("AttachmentUrl")
                    .HasMaxLength(1000)
                    .HasColumnType("character varying(1000)");

                b.Property<string>("AuthorDisplayName")
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<string>("AuthorUserId")
                    .HasMaxLength(450)
                    .HasColumnType("character varying(450)");

                b.Property<string>("Body")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<string>("CanonicalUrl")
                    .HasMaxLength(1000)
                    .HasColumnType("character varying(1000)");

                b.Property<Guid?>("CategoryId")
                    .HasColumnType("uuid");

                b.Property<string>("CoverImageUrl")
                    .HasMaxLength(1000)
                    .HasColumnType("character varying(1000)");

                b.Property<DateTime>("CreatedAtUtc")
                    .HasColumnType("timestamp with time zone");

                b.Property<DateTime?>("EventEndAtUtc")
                    .HasColumnType("timestamp with time zone");

                b.Property<DateTime?>("EventStartAtUtc")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("Excerpt")
                    .HasMaxLength(1000)
                    .HasColumnType("character varying(1000)");

                b.Property<string>("GalleryJson")
                    .HasColumnType("text");

                b.Property<string>("Kind")
                    .IsRequired()
                    .HasMaxLength(32)
                    .HasColumnType("character varying(32)");

                b.Property<string>("Location")
                    .HasMaxLength(300)
                    .HasColumnType("character varying(300)");

                b.Property<string>("MetaDescription")
                    .HasMaxLength(500)
                    .HasColumnType("character varying(500)");

                b.Property<string>("MetaTitle")
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<string>("OgDescription")
                    .HasMaxLength(500)
                    .HasColumnType("character varying(500)");

                b.Property<string>("OgImageUrl")
                    .HasMaxLength(1000)
                    .HasColumnType("character varying(1000)");

                b.Property<string>("OgTitle")
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<string>("OwnedByUserId")
                    .HasMaxLength(450)
                    .HasColumnType("character varying(450)");

                b.Property<DateTime?>("PublishedAtUtc")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("SeoKeywords")
                    .HasMaxLength(500)
                    .HasColumnType("character varying(500)");

                b.Property<string>("Slug")
                    .IsRequired()
                    .HasMaxLength(300)
                    .HasColumnType("character varying(300)");

                b.Property<string>("Status")
                    .IsRequired()
                    .HasMaxLength(32)
                    .HasColumnType("character varying(32)");

                b.Property<string>("Title")
                    .IsRequired()
                    .HasMaxLength(300)
                    .HasColumnType("character varying(300)");

                b.Property<DateTime?>("UpdatedAtUtc")
                    .HasColumnType("timestamp with time zone");

                b.HasKey("Id");

                b.HasIndex("CategoryId");

                b.HasIndex("Kind");

                b.HasIndex("OwnedByUserId", "Status");

                b.HasIndex("Slug")
                    .IsUnique();

                b.HasIndex("Status", "PublishedAtUtc");

                b.ToTable("Articles", "news");
            });

        modelBuilder.Entity("CMS.Modules.News.Domain.Entities.Category", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<DateTime>("CreatedAtUtc")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("Description")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasColumnType("text")
                    .HasDefaultValue("");

                b.Property<string>("ImageUrl")
                    .HasMaxLength(1000)
                    .HasColumnType("character varying(1000)");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<string>("Slug")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<DateTime?>("UpdatedAtUtc")
                    .HasColumnType("timestamp with time zone");

                b.HasKey("Id");

                b.HasIndex("Slug")
                    .IsUnique();

                b.ToTable("Categories", "news");
            });

        modelBuilder.Entity("CMS.Modules.News.Domain.Entities.Article", b =>
            {
                b.HasOne("CMS.Modules.News.Domain.Entities.Category", "Category")
                    .WithMany("Articles")
                    .HasForeignKey("CategoryId")
                    .OnDelete(DeleteBehavior.SetNull);

                b.Navigation("Category");
            });

        modelBuilder.Entity("CMS.Modules.News.Domain.Entities.Category", b =>
            {
                b.Navigation("Articles");
            });
#pragma warning restore 612, 618
    }
}

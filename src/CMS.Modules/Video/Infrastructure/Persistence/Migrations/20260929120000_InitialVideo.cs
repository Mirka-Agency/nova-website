using System;
using CMS.Modules.Video.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Video.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VideoDbContext))]
[Migration("20260929120000_InitialVideo")]
public class InitialVideo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "video");

        migrationBuilder.CreateTable(
            name: "Categories",
            schema: "video",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "VideoItems",
            schema: "video",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Body = table.Column<string>(type: "text", nullable: false),
                Excerpt = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                VideoUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CoverImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AuthorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                AuthorDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                OwnedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                MetaTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                MetaDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                SeoKeywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CanonicalUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                OgTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                OgDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                OgImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VideoItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_VideoItems_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalSchema: "video",
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Slug",
            schema: "video",
            table: "Categories",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_VideoItems_CategoryId",
            schema: "video",
            table: "VideoItems",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_VideoItems_OwnedByUserId_Status",
            schema: "video",
            table: "VideoItems",
            columns: new[] { "OwnedByUserId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_VideoItems_Slug",
            schema: "video",
            table: "VideoItems",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_VideoItems_Status_PublishedAtUtc",
            schema: "video",
            table: "VideoItems",
            columns: new[] { "Status", "PublishedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "VideoItems",
            schema: "video");

        migrationBuilder.DropTable(
            name: "Categories",
            schema: "video");
    }
}

using System;
using CMS.Modules.Team.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Team.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TeamDbContext))]
[Migration("20260929140000_InitialTeam")]
public class InitialTeam : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "team");

        migrationBuilder.CreateTable(
            name: "Categories",
            schema: "team",
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
            name: "TeamItems",
            schema: "team",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Body = table.Column<string>(type: "text", nullable: false),
                Excerpt = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                table.PrimaryKey("PK_TeamItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_TeamItems_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalSchema: "team",
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Slug",
            schema: "team",
            table: "Categories",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TeamItems_CategoryId",
            schema: "team",
            table: "TeamItems",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_TeamItems_OwnedByUserId_Status",
            schema: "team",
            table: "TeamItems",
            columns: new[] { "OwnedByUserId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_TeamItems_Slug",
            schema: "team",
            table: "TeamItems",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TeamItems_Status_PublishedAtUtc",
            schema: "team",
            table: "TeamItems",
            columns: new[] { "Status", "PublishedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TeamItems",
            schema: "team");

        migrationBuilder.DropTable(
            name: "Categories",
            schema: "team");
    }
}

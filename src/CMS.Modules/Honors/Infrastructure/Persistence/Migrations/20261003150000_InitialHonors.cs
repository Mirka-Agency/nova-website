using System;
using CMS.Modules.Honors.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Honors.Infrastructure.Persistence.Migrations;

[DbContext(typeof(HonorsDbContext))]
[Migration("20261003150000_InitialHonors")]
public class InitialHonors : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "honors");

        migrationBuilder.CreateTable(
            name: "HonorItems",
            schema: "honors",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                AltText = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HonorItems", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HonorItems_IsPublished_SortOrder",
            schema: "honors",
            table: "HonorItems",
            columns: new[] { "IsPublished", "SortOrder" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HonorItems",
            schema: "honors");
    }
}

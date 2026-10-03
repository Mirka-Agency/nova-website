using System;
using CMS.Modules.Voices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Voices.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VoicesDbContext))]
[Migration("20261003160000_InitialVoices")]
public class InitialVoices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "voices");

        migrationBuilder.CreateTable(
            name: "VoiceItems",
            schema: "voices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Subtitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                AudioUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VoiceItems", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_VoiceItems_IsPublished_SortOrder",
            schema: "voices",
            table: "VoiceItems",
            columns: new[] { "IsPublished", "SortOrder" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "VoiceItems",
            schema: "voices");
    }
}

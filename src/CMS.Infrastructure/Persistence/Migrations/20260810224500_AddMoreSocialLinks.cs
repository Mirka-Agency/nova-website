using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810224500_AddMoreSocialLinks")]
public class AddMoreSocialLinks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AparatUrl",
            schema: "core",
            table: "SiteSettings",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FacebookUrl",
            schema: "core",
            table: "SiteSettings",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "YouTubeUrl",
            schema: "core",
            table: "SiteSettings",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "WhatsAppUrl",
            schema: "core",
            table: "SiteSettings",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AparatUrl", schema: "core", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "FacebookUrl", schema: "core", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "YouTubeUrl", schema: "core", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "WhatsAppUrl", schema: "core", table: "SiteSettings");
    }
}

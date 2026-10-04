using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261004103000_AddSiteBusinessHoursAndPrivacy")]
public class AddSiteBusinessHoursAndPrivacy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BusinessHours",
            schema: "core",
            table: "SiteSettings",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PrivacyHtml",
            schema: "core",
            table: "SiteSettings",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BusinessHours", schema: "core", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "PrivacyHtml", schema: "core", table: "SiteSettings");
    }
}

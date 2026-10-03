using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260804100000_ExpandSiteSettings")]
    public partial class ExpandSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalyticsScript",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultOgImageUrl",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaviconUrl",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInUrl",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MaintenanceMode",
                schema: "core",
                table: "SiteSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceMessage",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelegramUrl",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitterUrl",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AnalyticsScript", schema: "core", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "DefaultOgImageUrl", schema: "core", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "FaviconUrl", schema: "core", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "InstagramUrl", schema: "core", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "LinkedInUrl", schema: "core", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "LogoUrl", schema: "core", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "MaintenanceMode", schema: "core", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "MaintenanceMessage", schema: "core", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "MetaDescription", schema: "core", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "MetaTitle", schema: "core", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "TelegramUrl", schema: "core", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "TwitterUrl", schema: "core", table: "SiteSettings");
        }
    }
}

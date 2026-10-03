using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260810140000_SiteScriptSlots")]
    public partial class SiteScriptSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeadScripts",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyOpenScripts",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyCloseScripts",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "core"."SiteSettings"
                SET "HeadScripts" = "AnalyticsScript"
                WHERE "AnalyticsScript" IS NOT NULL AND btrim("AnalyticsScript") <> '';
                """);

            migrationBuilder.DropColumn(
                name: "AnalyticsScript",
                schema: "core",
                table: "SiteSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalyticsScript",
                schema: "core",
                table: "SiteSettings",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "core"."SiteSettings"
                SET "AnalyticsScript" = left("HeadScripts", 8000)
                WHERE "HeadScripts" IS NOT NULL AND btrim("HeadScripts") <> '';
                """);

            migrationBuilder.DropColumn(
                name: "HeadScripts",
                schema: "core",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BodyOpenScripts",
                schema: "core",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BodyCloseScripts",
                schema: "core",
                table: "SiteSettings");
        }
    }
}

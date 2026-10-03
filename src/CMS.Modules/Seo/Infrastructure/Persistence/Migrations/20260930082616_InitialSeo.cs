using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Seo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSeo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "seo");

            migrationBuilder.CreateTable(
                name: "Documents",
                schema: "seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FocusKeyword = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RobotsIndex = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RobotsFollow = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SchemaType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SchemaJson = table.Column<string>(type: "text", nullable: true),
                    SeoScore = table.Column<int>(type: "integer", nullable: true),
                    AnalysisJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Redirects",
                schema: "seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ToUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Redirects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                schema: "seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OrganizationUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OrganizationLogoUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DefaultSchemaType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RobotsTxtExtra = table.Column<string>(type: "text", nullable: true),
                    TwitterSiteHandle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EnableBrokenLinkChecks = table.Column<bool>(type: "boolean", nullable: false),
                    SitemapEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ContentType_ContentId",
                schema: "seo",
                table: "Documents",
                columns: new[] { "ContentType", "ContentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Redirects_FromPath",
                schema: "seo",
                table: "Redirects",
                column: "FromPath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Redirects_IsActive_FromPath",
                schema: "seo",
                table: "Redirects",
                columns: new[] { "IsActive", "FromPath" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents",
                schema: "seo");

            migrationBuilder.DropTable(
                name: "Redirects",
                schema: "seo");

            migrationBuilder.DropTable(
                name: "SiteSettings",
                schema: "seo");
        }
    }
}

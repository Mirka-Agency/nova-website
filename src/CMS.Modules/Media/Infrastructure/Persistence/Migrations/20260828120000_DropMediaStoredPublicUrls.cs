using CMS.Modules.Media.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Media.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(MediaDbContext))]
    [Migration("20260828120000_DropMediaStoredPublicUrls")]
    public partial class DropMediaStoredPublicUrls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PublicUrl", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "ThumbnailPublicUrl", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "OriginalPublicUrl", schema: "media", table: "Assets");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicUrl",
                schema: "media",
                table: "Assets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPublicUrl",
                schema: "media",
                table: "Assets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalPublicUrl",
                schema: "media",
                table: "Assets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}

using CMS.Modules.Media.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Media.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(MediaDbContext))]
    [Migration("20260816120000_AddMediaImageOptimization")]
    public partial class AddMediaImageOptimization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Height",
                schema: "media",
                table: "Assets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                schema: "media",
                table: "Assets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailObjectKey",
                schema: "media",
                table: "Assets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPublicUrl",
                schema: "media",
                table: "Assets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                schema: "media",
                table: "Assets",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalContentType",
                schema: "media",
                table: "Assets",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OriginalSizeBytes",
                schema: "media",
                table: "Assets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalObjectKey",
                schema: "media",
                table: "Assets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalPublicUrl",
                schema: "media",
                table: "Assets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantsJson",
                schema: "media",
                table: "Assets",
                type: "jsonb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Height", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "Width", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "ThumbnailObjectKey", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "ThumbnailPublicUrl", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "OriginalFileName", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "OriginalContentType", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "OriginalSizeBytes", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "OriginalObjectKey", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "OriginalPublicUrl", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "VariantsJson", schema: "media", table: "Assets");
        }
    }
}

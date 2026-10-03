using CMS.Modules.Media.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Media.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(MediaDbContext))]
    [Migration("20260805140000_AddMediaMetadata")]
    public partial class AddMediaMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AltText",
                schema: "media",
                table: "Assets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Caption",
                schema: "media",
                table: "Assets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "media",
                table: "Assets",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "media",
                table: "Assets",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AltText", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "Caption", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "Description", schema: "media", table: "Assets");
            migrationBuilder.DropColumn(name: "Title", schema: "media", table: "Assets");
        }
    }
}

using CMS.Modules.Video.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Video.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VideoDbContext))]
[Migration("20261007120400_AddVideoItemCoverImageAlt")]
public class AddVideoItemCoverImageAlt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CoverImageAlt",
            schema: "video",
            table: "VideoItems",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CoverImageAlt",
            schema: "video",
            table: "VideoItems");
    }
}

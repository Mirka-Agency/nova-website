using CMS.Modules.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.News.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NewsDbContext))]
[Migration("20261007120100_AddArticleCoverImageAlt")]
public class AddArticleCoverImageAlt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CoverImageAlt",
            schema: "news",
            table: "Articles",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CoverImageAlt",
            schema: "news",
            table: "Articles");
    }
}

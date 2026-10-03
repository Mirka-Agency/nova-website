using CMS.Modules.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.News.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NewsDbContext))]
[Migration("20261003140000_AddArticleGalleryJson")]
public class AddArticleGalleryJson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "GalleryJson",
            schema: "news",
            table: "Articles",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "GalleryJson",
            schema: "news",
            table: "Articles");
    }
}

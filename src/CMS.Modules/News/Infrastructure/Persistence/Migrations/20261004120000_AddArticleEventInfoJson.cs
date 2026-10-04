using CMS.Modules.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.News.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NewsDbContext))]
[Migration("20261004120000_AddArticleEventInfoJson")]
public class AddArticleEventInfoJson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "EventInfoJson",
            schema: "news",
            table: "Articles",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EventInfoJson",
            schema: "news",
            table: "Articles");
    }
}

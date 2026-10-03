using CMS.Modules.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.News.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NewsDbContext))]
[Migration("20260821210000_AddPerfIndexes")]
public class AddPerfIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Articles_Status_PublishedAtUtc",
            schema: "news",
            table: "Articles",
            columns: new[] { "Status", "PublishedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Articles_Status_PublishedAtUtc",
            schema: "news",
            table: "Articles");
    }
}

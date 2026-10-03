using CMS.Modules.Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Comments.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommentsDbContext))]
[Migration("20260826075000_AddEnableOnProductCategories")]
public class AddEnableOnProductCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EnableOnProductCategories",
            schema: "comments",
            table: "Settings",
            type: "boolean",
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EnableOnProductCategories",
            schema: "comments",
            table: "Settings");
    }
}

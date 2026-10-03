using CMS.Modules.Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Blog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20260810231500_AddPostOwnedByUserId")]
public class AddPostOwnedByUserId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "OwnedByUserId",
            schema: "blog",
            table: "Posts",
            type: "character varying(450)",
            maxLength: 450,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Posts_OwnedByUserId_Status",
            schema: "blog",
            table: "Posts",
            columns: new[] { "OwnedByUserId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Posts_OwnedByUserId_Status",
            schema: "blog",
            table: "Posts");

        migrationBuilder.DropColumn(
            name: "OwnedByUserId",
            schema: "blog",
            table: "Posts");
    }
}

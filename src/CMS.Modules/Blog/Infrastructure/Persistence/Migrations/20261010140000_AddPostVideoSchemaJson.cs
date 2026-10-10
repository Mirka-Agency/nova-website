using CMS.Modules.Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Blog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20261010140000_AddPostVideoSchemaJson")]
public class AddPostVideoSchemaJson : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "VideoSchemaJson",
            schema: "blog",
            table: "Posts",
            type: "text",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "VideoSchemaJson",
            schema: "blog",
            table: "Posts");
    }
}

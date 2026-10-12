using CMS.Modules.Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Blog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20261012120000_AddPostSourcesJson")]
public class AddPostSourcesJson : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SourcesJson",
            schema: "blog",
            table: "Posts",
            type: "text",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SourcesJson",
            schema: "blog",
            table: "Posts");
    }
}

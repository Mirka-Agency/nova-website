using CMS.Modules.Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Blog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20261015140000_AddPostRelatedPostsJson")]
public class AddPostRelatedPostsJson : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "RelatedPostsJson",
            schema: "blog",
            table: "Posts",
            type: "text",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RelatedPostsJson",
            schema: "blog",
            table: "Posts");
    }
}

using CMS.Modules.Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Blog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20261011160000_AddPostContentGuideJson")]
public class AddPostContentGuideJson : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ContentGuideJson",
            schema: "blog",
            table: "Posts",
            type: "text",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ContentGuideJson",
            schema: "blog",
            table: "Posts");
    }
}

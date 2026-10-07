using CMS.Modules.Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Blog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20261007120000_AddPostCoverImageAlt")]
public class AddPostCoverImageAlt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CoverImageAlt",
            schema: "blog",
            table: "Posts",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CoverImageAlt",
            schema: "blog",
            table: "Posts");
    }
}

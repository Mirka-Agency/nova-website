using CMS.Modules.Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Blog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20260810223000_AddPostSeoAuthorExcerpt")]
public class AddPostSeoAuthorExcerpt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AuthorDisplayName",
            schema: "blog",
            table: "Posts",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AuthorUserId",
            schema: "blog",
            table: "Posts",
            type: "character varying(450)",
            maxLength: 450,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CanonicalUrl",
            schema: "blog",
            table: "Posts",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Excerpt",
            schema: "blog",
            table: "Posts",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MetaDescription",
            schema: "blog",
            table: "Posts",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MetaTitle",
            schema: "blog",
            table: "Posts",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OgDescription",
            schema: "blog",
            table: "Posts",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OgImageUrl",
            schema: "blog",
            table: "Posts",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OgTitle",
            schema: "blog",
            table: "Posts",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SeoKeywords",
            schema: "blog",
            table: "Posts",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AuthorDisplayName", schema: "blog", table: "Posts");
        migrationBuilder.DropColumn(name: "AuthorUserId", schema: "blog", table: "Posts");
        migrationBuilder.DropColumn(name: "CanonicalUrl", schema: "blog", table: "Posts");
        migrationBuilder.DropColumn(name: "Excerpt", schema: "blog", table: "Posts");
        migrationBuilder.DropColumn(name: "MetaDescription", schema: "blog", table: "Posts");
        migrationBuilder.DropColumn(name: "MetaTitle", schema: "blog", table: "Posts");
        migrationBuilder.DropColumn(name: "OgDescription", schema: "blog", table: "Posts");
        migrationBuilder.DropColumn(name: "OgImageUrl", schema: "blog", table: "Posts");
        migrationBuilder.DropColumn(name: "OgTitle", schema: "blog", table: "Posts");
        migrationBuilder.DropColumn(name: "SeoKeywords", schema: "blog", table: "Posts");
    }
}

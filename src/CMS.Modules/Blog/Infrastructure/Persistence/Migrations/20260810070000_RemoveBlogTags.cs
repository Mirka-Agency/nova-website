using CMS.Modules.Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Blog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BlogDbContext))]
[Migration("20260810070000_RemoveBlogTags")]
public class RemoveBlogTags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PostTags",
            schema: "blog");

        migrationBuilder.DropTable(
            name: "Tags",
            schema: "blog");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tags",
            schema: "blog",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Tags", x => x.Id));

        migrationBuilder.CreateTable(
            name: "PostTags",
            schema: "blog",
            columns: table => new
            {
                PostId = table.Column<Guid>(type: "uuid", nullable: false),
                TagId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PostTags", x => new { x.PostId, x.TagId });
                table.ForeignKey(
                    name: "FK_PostTags_Posts_PostId",
                    column: x => x.PostId,
                    principalSchema: "blog",
                    principalTable: "Posts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PostTags_Tags_TagId",
                    column: x => x.TagId,
                    principalSchema: "blog",
                    principalTable: "Tags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PostTags_TagId",
            schema: "blog",
            table: "PostTags",
            column: "TagId");

        migrationBuilder.CreateIndex(
            name: "IX_Tags_Slug",
            schema: "blog",
            table: "Tags",
            column: "Slug",
            unique: true);
    }
}

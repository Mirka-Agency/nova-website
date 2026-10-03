using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260810070000_RemoveProductTags")]
public class RemoveProductTags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ProductTags",
            schema: "shop");

        migrationBuilder.DropTable(
            name: "Tags",
            schema: "shop");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tags",
            schema: "shop",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Tags", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ProductTags",
            schema: "shop",
            columns: table => new
            {
                ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                TagId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductTags", x => new { x.ProductId, x.TagId });
                table.ForeignKey(
                    name: "FK_ProductTags_Products_ProductId",
                    column: x => x.ProductId,
                    principalSchema: "shop",
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProductTags_Tags_TagId",
                    column: x => x.TagId,
                    principalSchema: "shop",
                    principalTable: "Tags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProductTags_TagId",
            schema: "shop",
            table: "ProductTags",
            column: "TagId");

        migrationBuilder.CreateIndex(
            name: "IX_Tags_Slug",
            schema: "shop",
            table: "Tags",
            column: "Slug",
            unique: true);
    }
}

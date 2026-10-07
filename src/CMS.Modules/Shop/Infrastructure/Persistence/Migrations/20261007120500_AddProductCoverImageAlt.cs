using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20261007120500_AddProductCoverImageAlt")]
public class AddProductCoverImageAlt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CoverImageAlt",
            schema: "shop",
            table: "Products",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CoverImageAlt",
            schema: "shop",
            table: "Products");
    }
}

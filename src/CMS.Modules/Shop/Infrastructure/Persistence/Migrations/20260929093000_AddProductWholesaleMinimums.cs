using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260929093000_AddProductWholesaleMinimums")]
public class AddProductWholesaleMinimums : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "WholesaleMinimumOrderQuantity",
            schema: "shop",
            table: "Products",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "WholesaleMinimumOrderAmount",
            schema: "shop",
            table: "Products",
            type: "numeric(18,2)",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "WholesaleMinimumOrderQuantity",
            schema: "shop",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "WholesaleMinimumOrderAmount",
            schema: "shop",
            table: "Products");
    }
}

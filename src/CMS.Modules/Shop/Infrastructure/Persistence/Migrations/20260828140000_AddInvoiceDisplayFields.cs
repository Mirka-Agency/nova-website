using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260828140000_AddInvoiceDisplayFields")]
public class AddInvoiceDisplayFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "InvoiceBuyerDisplayFields",
            schema: "shop",
            table: "Settings",
            type: "integer",
            nullable: false,
            defaultValue: 89);

        migrationBuilder.AddColumn<int>(
            name: "InvoiceSellerDisplayFields",
            schema: "shop",
            table: "Settings",
            type: "integer",
            nullable: false,
            defaultValue: 273);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "InvoiceBuyerDisplayFields", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "InvoiceSellerDisplayFields", schema: "shop", table: "Settings");
    }
}

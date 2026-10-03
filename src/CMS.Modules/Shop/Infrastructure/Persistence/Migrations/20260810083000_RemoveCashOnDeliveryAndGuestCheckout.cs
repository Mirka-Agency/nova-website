using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260810083000_RemoveCashOnDeliveryAndGuestCheckout")]
public class RemoveCashOnDeliveryAndGuestCheckout : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EnableCashOnDelivery",
            schema: "shop",
            table: "Settings");

        migrationBuilder.DropColumn(
            name: "EnableGuestCheckout",
            schema: "shop",
            table: "Settings");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EnableCashOnDelivery",
            schema: "shop",
            table: "Settings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EnableGuestCheckout",
            schema: "shop",
            table: "Settings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }
}

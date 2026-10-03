using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260814100000_AddVatSettings")]
public class AddVatSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EnableVat",
            schema: "shop",
            table: "Settings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<decimal>(
            name: "VatPercent",
            schema: "shop",
            table: "Settings",
            type: "numeric(5,2)",
            precision: 5,
            scale: 2,
            nullable: false,
            defaultValue: 10m);

        migrationBuilder.AddColumn<decimal>(
            name: "VatAmount",
            schema: "shop",
            table: "Orders",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "VatPercent",
            schema: "shop",
            table: "Orders",
            type: "numeric(5,2)",
            precision: 5,
            scale: 2,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EnableVat",
            schema: "shop",
            table: "Settings");

        migrationBuilder.DropColumn(
            name: "VatPercent",
            schema: "shop",
            table: "Settings");

        migrationBuilder.DropColumn(
            name: "VatAmount",
            schema: "shop",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "VatPercent",
            schema: "shop",
            table: "Orders");
    }
}

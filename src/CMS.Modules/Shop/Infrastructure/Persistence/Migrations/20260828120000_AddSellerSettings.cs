using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260828120000_AddSellerSettings")]
public class AddSellerSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SellerAddress",
            schema: "shop",
            table: "Settings",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SellerCity",
            schema: "shop",
            table: "Settings",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SellerName",
            schema: "shop",
            table: "Settings",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SellerPhone",
            schema: "shop",
            table: "Settings",
            type: "character varying(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SellerPostalCode",
            schema: "shop",
            table: "Settings",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SellerProvince",
            schema: "shop",
            table: "Settings",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SellerAddress", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "SellerCity", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "SellerName", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "SellerPhone", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "SellerPostalCode", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "SellerProvince", schema: "shop", table: "Settings");
    }
}

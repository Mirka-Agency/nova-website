using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260828130000_AddSellerInvoiceFields")]
public class AddSellerInvoiceFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SellerEconomicCode",
            schema: "shop",
            table: "Settings",
            type: "character varying(14)",
            maxLength: 14,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SellerEmail",
            schema: "shop",
            table: "Settings",
            type: "character varying(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SellerNationalId",
            schema: "shop",
            table: "Settings",
            type: "character varying(11)",
            maxLength: 11,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SellerRegistrationNumber",
            schema: "shop",
            table: "Settings",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SellerEconomicCode", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "SellerEmail", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "SellerNationalId", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "SellerRegistrationNumber", schema: "shop", table: "Settings");
    }
}

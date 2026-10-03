using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260821220000_AddBankTransferDetails")]
public class AddBankTransferDetails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BankAccountHolderName",
            schema: "shop",
            table: "Settings",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BankName",
            schema: "shop",
            table: "Settings",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BankCardNumber",
            schema: "shop",
            table: "Settings",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BankShebaNumber",
            schema: "shop",
            table: "Settings",
            type: "character varying(34)",
            maxLength: 34,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BankTransferInstructions",
            schema: "shop",
            table: "Settings",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BankAccountHolderName", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "BankName", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "BankCardNumber", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "BankShebaNumber", schema: "shop", table: "Settings");
        migrationBuilder.DropColumn(name: "BankTransferInstructions", schema: "shop", table: "Settings");
    }
}

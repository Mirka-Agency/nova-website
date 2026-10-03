using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260813191000_AddCustomerAddressesAndOrderRecipients")]
public class AddCustomerAddressesAndOrderRecipients : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "RecipientName",
            schema: "shop",
            table: "Orders",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RecipientPhone",
            schema: "shop",
            table: "Orders",
            type: "character varying(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "CustomerAddresses",
            schema: "shop",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                RecipientPhone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                AddressLine = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerAddresses", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CustomerAddresses_UserId",
            schema: "shop",
            table: "CustomerAddresses",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_CustomerAddresses_UserId_IsDefault",
            schema: "shop",
            table: "CustomerAddresses",
            columns: new[] { "UserId", "IsDefault" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CustomerAddresses",
            schema: "shop");

        migrationBuilder.DropColumn(
            name: "RecipientName",
            schema: "shop",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "RecipientPhone",
            schema: "shop",
            table: "Orders");
    }
}

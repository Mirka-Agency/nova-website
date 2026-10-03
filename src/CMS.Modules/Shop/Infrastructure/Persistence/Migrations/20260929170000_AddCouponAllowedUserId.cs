using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260929170000_AddCouponAllowedUserId")]
public class AddCouponAllowedUserId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AllowedUserId",
            schema: "shop",
            table: "Coupons",
            type: "character varying(450)",
            maxLength: 450,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Coupons_AllowedUserId",
            schema: "shop",
            table: "Coupons",
            column: "AllowedUserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Coupons_AllowedUserId",
            schema: "shop",
            table: "Coupons");

        migrationBuilder.DropColumn(
            name: "AllowedUserId",
            schema: "shop",
            table: "Coupons");
    }
}

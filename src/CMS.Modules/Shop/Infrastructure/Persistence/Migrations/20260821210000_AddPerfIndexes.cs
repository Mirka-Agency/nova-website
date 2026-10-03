using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260821210000_AddPerfIndexes")]
public class AddPerfIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Products_Status",
            schema: "shop",
            table: "Products",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Products_Status_PublishedAtUtc",
            schema: "shop",
            table: "Products",
            columns: new[] { "Status", "PublishedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_Orders_UserId",
            schema: "shop",
            table: "Orders",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Orders_Status_PaymentStatus",
            schema: "shop",
            table: "Orders",
            columns: new[] { "Status", "PaymentStatus" });

        migrationBuilder.CreateIndex(
            name: "IX_PriceRules_IsActive",
            schema: "shop",
            table: "PriceRules",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_PriceRules_IsActive_ProductId_VariationId",
            schema: "shop",
            table: "PriceRules",
            columns: new[] { "IsActive", "ProductId", "VariationId" });

        migrationBuilder.CreateIndex(
            name: "IX_Reviews_ProductId_IsApproved",
            schema: "shop",
            table: "Reviews",
            columns: new[] { "ProductId", "IsApproved" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Products_Status", schema: "shop", table: "Products");
        migrationBuilder.DropIndex(name: "IX_Products_Status_PublishedAtUtc", schema: "shop", table: "Products");
        migrationBuilder.DropIndex(name: "IX_Orders_UserId", schema: "shop", table: "Orders");
        migrationBuilder.DropIndex(name: "IX_Orders_Status_PaymentStatus", schema: "shop", table: "Orders");
        migrationBuilder.DropIndex(name: "IX_PriceRules_IsActive", schema: "shop", table: "PriceRules");
        migrationBuilder.DropIndex(name: "IX_PriceRules_IsActive_ProductId_VariationId", schema: "shop", table: "PriceRules");
        migrationBuilder.DropIndex(name: "IX_Reviews_ProductId_IsApproved", schema: "shop", table: "Reviews");
    }
}

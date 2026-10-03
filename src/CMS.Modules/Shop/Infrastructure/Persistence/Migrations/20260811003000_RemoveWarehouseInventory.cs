using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260811003000_RemoveWarehouseInventory")]
public class RemoveWarehouseInventory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "StockMovements",
            schema: "shop");

        migrationBuilder.DropTable(
            name: "StockLevels",
            schema: "shop");

        migrationBuilder.DropTable(
            name: "Warehouses",
            schema: "shop");

        migrationBuilder.DropColumn(
            name: "EnableInventoryManagement",
            schema: "shop",
            table: "Settings");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EnableInventoryManagement",
            schema: "shop",
            table: "Settings",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateTable(
            name: "Warehouses",
            schema: "shop",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Warehouses", x => x.Id));

        migrationBuilder.CreateTable(
            name: "StockLevels",
            schema: "shop",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                Unlimited = table.Column<bool>(type: "boolean", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                VariationId = table.Column<Guid>(type: "uuid", nullable: true),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockLevels", x => x.Id);
                table.ForeignKey(
                    name: "FK_StockLevels_Products_ProductId",
                    column: x => x.ProductId,
                    principalSchema: "shop",
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_StockLevels_Variations_VariationId",
                    column: x => x.VariationId,
                    principalSchema: "shop",
                    principalTable: "Variations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_StockLevels_Warehouses_WarehouseId",
                    column: x => x.WarehouseId,
                    principalSchema: "shop",
                    principalTable: "Warehouses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "StockMovements",
            schema: "shop",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                MovementType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                QuantityDelta = table.Column<int>(type: "integer", nullable: false),
                Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                VariationId = table.Column<Guid>(type: "uuid", nullable: true),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockMovements", x => x.Id);
                table.ForeignKey(
                    name: "FK_StockMovements_Warehouses_WarehouseId",
                    column: x => x.WarehouseId,
                    principalSchema: "shop",
                    principalTable: "Warehouses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_StockLevels_ProductId",
            schema: "shop",
            table: "StockLevels",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_StockLevels_VariationId",
            schema: "shop",
            table: "StockLevels",
            column: "VariationId");

        migrationBuilder.CreateIndex(
            name: "IX_StockLevels_WarehouseId_ProductId_VariationId",
            schema: "shop",
            table: "StockLevels",
            columns: new[] { "WarehouseId", "ProductId", "VariationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StockMovements_CreatedAtUtc",
            schema: "shop",
            table: "StockMovements",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_StockMovements_WarehouseId",
            schema: "shop",
            table: "StockMovements",
            column: "WarehouseId");
    }
}

using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260814110000_AddShippingEstimatedDelivery")]
public class AddShippingEstimatedDelivery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "EstimatedDeliveryText",
            schema: "shop",
            table: "ShippingMethods",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ShippingEstimatedDelivery",
            schema: "shop",
            table: "Orders",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EstimatedDeliveryText",
            schema: "shop",
            table: "ShippingMethods");

        migrationBuilder.DropColumn(
            name: "ShippingEstimatedDelivery",
            schema: "shop",
            table: "Orders");
    }
}

using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260813210000_AddPendingPaymentReservation")]
public class AddPendingPaymentReservation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AbandonedPaymentReminderMinutes",
            schema: "shop",
            table: "Settings",
            type: "integer",
            nullable: false,
            defaultValue: 3);

        migrationBuilder.AddColumn<int>(
            name: "PendingPaymentTimeoutMinutes",
            schema: "shop",
            table: "Settings",
            type: "integer",
            nullable: false,
            defaultValue: 30);

        migrationBuilder.AddColumn<DateTime>(
            name: "PaymentExpiresAtUtc",
            schema: "shop",
            table: "Orders",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PaymentReminderSentAtUtc",
            schema: "shop",
            table: "Orders",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "StockReservationReleased",
            schema: "shop",
            table: "Orders",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_Orders_PaymentExpiresAtUtc",
            schema: "shop",
            table: "Orders",
            column: "PaymentExpiresAtUtc");

        migrationBuilder.Sql("""
            UPDATE shop."Orders"
            SET "PaymentExpiresAtUtc" = "CreatedAtUtc" + INTERVAL '30 minutes'
            WHERE "Status" = 'Pending'
              AND "PaymentStatus" = 'Unpaid'
              AND "StockReservationReleased" = FALSE
              AND "PaymentExpiresAtUtc" IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Orders_PaymentExpiresAtUtc",
            schema: "shop",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "AbandonedPaymentReminderMinutes",
            schema: "shop",
            table: "Settings");

        migrationBuilder.DropColumn(
            name: "PendingPaymentTimeoutMinutes",
            schema: "shop",
            table: "Settings");

        migrationBuilder.DropColumn(
            name: "PaymentExpiresAtUtc",
            schema: "shop",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "PaymentReminderSentAtUtc",
            schema: "shop",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "StockReservationReleased",
            schema: "shop",
            table: "Orders");
    }
}

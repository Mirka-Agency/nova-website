using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260814200000_AddIranLocationsAndShippingRateProvince")]
public class AddIranLocationsAndShippingRateProvince : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Province",
            schema: "shop",
            table: "ShippingRates",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "IranProvinces",
            schema: "shop",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IranProvinces", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "IranCities",
            schema: "shop",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProvinceId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IranCities", x => x.Id);
                table.ForeignKey(
                    name: "FK_IranCities_IranProvinces_ProvinceId",
                    column: x => x.ProvinceId,
                    principalSchema: "shop",
                    principalTable: "IranProvinces",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IranProvinces_Name",
            schema: "shop",
            table: "IranProvinces",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_IranCities_ProvinceId_Name",
            schema: "shop",
            table: "IranCities",
            columns: new[] { "ProvinceId", "Name" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IranCities",
            schema: "shop");

        migrationBuilder.DropTable(
            name: "IranProvinces",
            schema: "shop");

        migrationBuilder.DropColumn(
            name: "Province",
            schema: "shop",
            table: "ShippingRates");
    }
}

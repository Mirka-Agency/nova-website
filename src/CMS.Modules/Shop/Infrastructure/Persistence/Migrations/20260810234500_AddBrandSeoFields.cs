using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Shop.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260810234500_AddBrandSeoFields")]
public class AddBrandSeoFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MetaTitle",
            schema: "shop",
            table: "Brands",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MetaDescription",
            schema: "shop",
            table: "Brands",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SeoKeywords",
            schema: "shop",
            table: "Brands",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MetaTitle",
            schema: "shop",
            table: "Brands");

        migrationBuilder.DropColumn(
            name: "MetaDescription",
            schema: "shop",
            table: "Brands");

        migrationBuilder.DropColumn(
            name: "SeoKeywords",
            schema: "shop",
            table: "Brands");
    }
}

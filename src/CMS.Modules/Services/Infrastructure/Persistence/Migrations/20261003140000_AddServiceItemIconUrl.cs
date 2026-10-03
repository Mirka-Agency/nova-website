using CMS.Modules.Services.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Services.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ServicesDbContext))]
[Migration("20261003140000_AddServiceItemIconUrl")]
public class AddServiceItemIconUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IconUrl",
            schema: "services",
            table: "ServiceItems",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IconUrl",
            schema: "services",
            table: "ServiceItems");
    }
}

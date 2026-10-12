using CMS.Modules.Services.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Services.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ServicesDbContext))]
[Migration("20261012121000_AddServiceItemSourcesJson")]
public class AddServiceItemSourcesJson : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SourcesJson",
            schema: "services",
            table: "ServiceItems",
            type: "text",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SourcesJson",
            schema: "services",
            table: "ServiceItems");
    }
}

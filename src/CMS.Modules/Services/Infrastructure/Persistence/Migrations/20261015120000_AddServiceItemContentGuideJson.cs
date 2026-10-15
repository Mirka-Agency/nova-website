using CMS.Modules.Services.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Services.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ServicesDbContext))]
[Migration("20261015120000_AddServiceItemContentGuideJson")]
public class AddServiceItemContentGuideJson : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ContentGuideJson",
            schema: "services",
            table: "ServiceItems",
            type: "text",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ContentGuideJson",
            schema: "services",
            table: "ServiceItems");
    }
}

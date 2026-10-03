using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Forms.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSystemFormFlags : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsSystem",
            schema: "forms",
            table: "Forms",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_Forms_IsSystem",
            schema: "forms",
            table: "Forms",
            column: "IsSystem");

        migrationBuilder.AddColumn<bool>(
            name: "IsSystem",
            schema: "forms",
            table: "Fields",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Forms_IsSystem",
            schema: "forms",
            table: "Forms");

        migrationBuilder.DropColumn(
            name: "IsSystem",
            schema: "forms",
            table: "Forms");

        migrationBuilder.DropColumn(
            name: "IsSystem",
            schema: "forms",
            table: "Fields");
    }
}

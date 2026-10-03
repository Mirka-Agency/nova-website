using CMS.Modules.Team.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Team.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TeamDbContext))]
[Migration("20261003124500_AddTeamHighlights")]
public class AddTeamHighlights : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Highlights",
            schema: "team",
            table: "TeamItems",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Highlights",
            schema: "team",
            table: "TeamItems");
    }
}

using CMS.Modules.Team.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Team.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TeamDbContext))]
[Migration("20261003134500_AddTeamScientificActivityJson")]
public class AddTeamScientificActivityJson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ScientificActivityJson",
            schema: "team",
            table: "TeamItems",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ScientificActivityJson",
            schema: "team",
            table: "TeamItems");
    }
}

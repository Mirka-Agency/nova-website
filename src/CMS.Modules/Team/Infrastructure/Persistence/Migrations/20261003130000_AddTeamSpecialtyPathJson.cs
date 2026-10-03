using CMS.Modules.Team.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Team.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TeamDbContext))]
[Migration("20261003130000_AddTeamSpecialtyPathJson")]
public class AddTeamSpecialtyPathJson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SpecialtyPathJson",
            schema: "team",
            table: "TeamItems",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SpecialtyPathJson",
            schema: "team",
            table: "TeamItems");
    }
}

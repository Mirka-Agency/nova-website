using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260813190000_AddOtpChallenges")]
public class AddOtpChallenges : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OtpChallenges",
            schema: "core",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OtpChallenges", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OtpChallenges_ExpiresAtUtc",
            schema: "core",
            table: "OtpChallenges",
            column: "ExpiresAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_OtpChallenges_Phone",
            schema: "core",
            table: "OtpChallenges",
            column: "Phone",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OtpChallenges",
            schema: "core");
    }
}

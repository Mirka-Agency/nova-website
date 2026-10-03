using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260821190000_AddMessageLogs")]
public class AddMessageLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MessageLogs",
            schema: "core",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Channel = table.Column<byte>(type: "smallint", nullable: false),
                Status = table.Column<byte>(type: "smallint", nullable: false),
                Recipient = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                RecipientCount = table.Column<int>(type: "integer", nullable: false),
                Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                BodyPreview = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                ProviderMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MessageLogs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MessageLogs_CreatedAtUtc",
            schema: "core",
            table: "MessageLogs",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_MessageLogs_Channel_CreatedAtUtc",
            schema: "core",
            table: "MessageLogs",
            columns: new[] { "Channel", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MessageLogs",
            schema: "core");
    }
}

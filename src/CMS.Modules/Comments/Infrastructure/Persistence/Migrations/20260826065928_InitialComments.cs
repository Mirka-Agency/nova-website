using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Comments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "comments");

            migrationBuilder.CreateTable(
                name: "Comments",
                schema: "comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AuthorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AuthorPhone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    EnableOnBlog = table.Column<bool>(type: "boolean", nullable: false),
                    EnableOnEvents = table.Column<bool>(type: "boolean", nullable: false),
                    EnableOnProducts = table.Column<bool>(type: "boolean", nullable: false),
                    ShowEmail = table.Column<bool>(type: "boolean", nullable: false),
                    RequireEmail = table.Column<bool>(type: "boolean", nullable: false),
                    ShowPhone = table.Column<bool>(type: "boolean", nullable: false),
                    RequirePhone = table.Column<bool>(type: "boolean", nullable: false),
                    EnableCaptcha = table.Column<bool>(type: "boolean", nullable: false),
                    CaptchaProvider = table.Column<int>(type: "integer", nullable: false),
                    SimpleCaptchaPrompt = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SimpleCaptchaExpected = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CreatedAtUtc",
                schema: "comments",
                table: "Comments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PublishedAtUtc",
                schema: "comments",
                table: "Comments",
                column: "PublishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TargetType_TargetId_Status",
                schema: "comments",
                table: "Comments",
                columns: new[] { "TargetType", "TargetId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comments",
                schema: "comments");

            migrationBuilder.DropTable(
                name: "Settings",
                schema: "comments");
        }
    }
}

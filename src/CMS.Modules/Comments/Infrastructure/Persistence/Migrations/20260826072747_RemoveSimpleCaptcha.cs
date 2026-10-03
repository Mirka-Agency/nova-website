using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Comments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSimpleCaptcha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SimpleCaptchaExpected",
                schema: "comments",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SimpleCaptchaPrompt",
                schema: "comments",
                table: "Settings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SimpleCaptchaExpected",
                schema: "comments",
                table: "Settings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimpleCaptchaPrompt",
                schema: "comments",
                table: "Settings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }
    }
}

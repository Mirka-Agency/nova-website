using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Forms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyFormSettingsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoReplyBody",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "AutoReplyEmailFieldKey",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "AutoReplyEnabled",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "AutoReplySubject",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "EnableCaptcha",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "NotifyEmail",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "NotifyEmailSubject",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "NotifyReplyToFieldKey",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "NotifySenderName",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "RedirectUrl",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "SendEmailNotification",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "SubmitButtonText",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "SuccessMessage",
                schema: "forms",
                table: "Forms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AutoReplyBody",
                schema: "forms",
                table: "Forms",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoReplyEmailFieldKey",
                schema: "forms",
                table: "Forms",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoReplyEnabled",
                schema: "forms",
                table: "Forms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AutoReplySubject",
                schema: "forms",
                table: "Forms",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableCaptcha",
                schema: "forms",
                table: "Forms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NotifyEmail",
                schema: "forms",
                table: "Forms",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotifyEmailSubject",
                schema: "forms",
                table: "Forms",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotifyReplyToFieldKey",
                schema: "forms",
                table: "Forms",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotifySenderName",
                schema: "forms",
                table: "Forms",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RedirectUrl",
                schema: "forms",
                table: "Forms",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SendEmailNotification",
                schema: "forms",
                table: "Forms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SubmitButtonText",
                schema: "forms",
                table: "Forms",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "ارسال");

            migrationBuilder.AddColumn<string>(
                name: "SuccessMessage",
                schema: "forms",
                table: "Forms",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}

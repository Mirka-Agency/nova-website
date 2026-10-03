using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Forms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandFormsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "forms",
                table: "Forms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE forms."Forms"
                SET "Status" = CASE WHEN "IsPublished" THEN 1 ELSE 0 END;
                """);

            migrationBuilder.DropColumn(
                name: "IsPublished",
                schema: "forms",
                table: "Forms");

            migrationBuilder.AddColumn<string>(
                name: "SuccessMessage",
                schema: "forms",
                table: "Forms",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RedirectUrl",
                schema: "forms",
                table: "Forms",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmitButtonText",
                schema: "forms",
                table: "Forms",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "ارسال");

            migrationBuilder.AddColumn<string>(
                name: "NotifyEmailSubject",
                schema: "forms",
                table: "Forms",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotifySenderName",
                schema: "forms",
                table: "Forms",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotifyReplyToFieldKey",
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
                name: "EnableCaptcha",
                schema: "forms",
                table: "Forms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Forms_Status",
                schema: "forms",
                table: "Forms",
                column: "Status");

            migrationBuilder.AddColumn<string>(
                name: "Placeholder",
                schema: "forms",
                table: "Fields",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelpText",
                schema: "forms",
                table: "Fields",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettingsJson",
                schema: "forms",
                table: "Fields",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fields_FormId",
                schema: "forms",
                table: "Fields",
                column: "FormId");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "forms",
                table: "Submissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_Status",
                schema: "forms",
                table: "Submissions",
                column: "Status");

            migrationBuilder.AddColumn<Guid>(
                name: "FieldId",
                schema: "forms",
                table: "SubmissionValues",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionValues_FieldId",
                schema: "forms",
                table: "SubmissionValues",
                column: "FieldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionValues_FieldId",
                schema: "forms",
                table: "SubmissionValues");

            migrationBuilder.DropColumn(
                name: "FieldId",
                schema: "forms",
                table: "SubmissionValues");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_Status",
                schema: "forms",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "forms",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Fields_FormId",
                schema: "forms",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "SettingsJson",
                schema: "forms",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "HelpText",
                schema: "forms",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "Placeholder",
                schema: "forms",
                table: "Fields");

            migrationBuilder.DropIndex(
                name: "IX_Forms_Status",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "EnableCaptcha",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "AutoReplyEmailFieldKey",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "AutoReplyBody",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "AutoReplySubject",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "AutoReplyEnabled",
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
                name: "NotifyEmailSubject",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "SubmitButtonText",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "RedirectUrl",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "SuccessMessage",
                schema: "forms",
                table: "Forms");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                schema: "forms",
                table: "Forms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE forms."Forms"
                SET "IsPublished" = ("Status" = 1);
                """);

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "forms",
                table: "Forms");
        }
    }
}

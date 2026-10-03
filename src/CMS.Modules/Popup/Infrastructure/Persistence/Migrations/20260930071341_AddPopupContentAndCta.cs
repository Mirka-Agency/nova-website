using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Popup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPopupContentAndCta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyText",
                schema: "popup",
                table: "Popups",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CtaAction",
                schema: "popup",
                table: "Popups",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "none");

            migrationBuilder.AddColumn<Guid>(
                name: "CtaTargetPopupId",
                schema: "popup",
                table: "Popups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CtaText",
                schema: "popup",
                table: "Popups",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CtaUrl",
                schema: "popup",
                table: "Popups",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "popup",
                table: "Popups",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyText",
                schema: "popup",
                table: "Popups");

            migrationBuilder.DropColumn(
                name: "CtaAction",
                schema: "popup",
                table: "Popups");

            migrationBuilder.DropColumn(
                name: "CtaTargetPopupId",
                schema: "popup",
                table: "Popups");

            migrationBuilder.DropColumn(
                name: "CtaText",
                schema: "popup",
                table: "Popups");

            migrationBuilder.DropColumn(
                name: "CtaUrl",
                schema: "popup",
                table: "Popups");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "popup",
                table: "Popups");
        }
    }
}

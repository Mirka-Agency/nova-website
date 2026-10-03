using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Popup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPopup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "popup");

            migrationBuilder.CreateTable(
                name: "Popups",
                schema: "popup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContentHtml = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: true),
                    TriggerType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TriggerDelaySeconds = table.Column<int>(type: "integer", nullable: true),
                    TriggerScrollPercent = table.Column<int>(type: "integer", nullable: true),
                    TriggerSelector = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TriggerConfigJson = table.Column<string>(type: "text", nullable: true),
                    ShowOverlay = table.Column<bool>(type: "boolean", nullable: false),
                    ShowCloseButton = table.Column<bool>(type: "boolean", nullable: false),
                    CloseOnOverlayClick = table.Column<bool>(type: "boolean", nullable: false),
                    CloseOnEscape = table.Column<bool>(type: "boolean", nullable: false),
                    LockBodyScroll = table.Column<bool>(type: "boolean", nullable: false),
                    EnableContentScroll = table.Column<bool>(type: "boolean", nullable: false),
                    Frequency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PageTargetMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PagePaths = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    ExtensionSettingsJson = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Popups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Popups_IsActive_SortOrder",
                schema: "popup",
                table: "Popups",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Popups_Slug",
                schema: "popup",
                table: "Popups",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Popups",
                schema: "popup");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Forms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFormVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Key",
                schema: "forms",
                table: "Forms",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DraftVersionId",
                schema: "forms",
                table: "Forms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedVersionId",
                schema: "forms",
                table: "Forms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FormVersionId",
                schema: "forms",
                table: "Submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FormVersions",
                schema: "forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    SchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormVersions_Forms_FormId",
                        column: x => x.FormId,
                        principalSchema: "forms",
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                UPDATE forms."Forms"
                SET "Key" = lower(replace("Slug", '-', '_'))
                WHERE "Key" = '' OR "Key" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                WITH ranked AS (
                    SELECT "Id",
                           "Key",
                           ROW_NUMBER() OVER (PARTITION BY "Key" ORDER BY "CreatedAtUtc", "Id") AS rn
                    FROM forms."Forms"
                )
                UPDATE forms."Forms" f
                SET "Key" = left(f."Key" || '_' || ranked.rn::text, 100)
                FROM ranked
                WHERE f."Id" = ranked."Id" AND ranked.rn > 1;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO forms."FormVersions" ("Id", "FormId", "VersionNumber", "State", "SchemaJson", "CreatedAtUtc", "UpdatedAtUtc")
                SELECT gen_random_uuid(),
                       f."Id",
                       1,
                       CASE WHEN f."Status" = 1 THEN 1 ELSE 0 END,
                       '{}'::jsonb,
                       NOW() AT TIME ZONE 'utc',
                       NULL
                FROM forms."Forms" f
                WHERE NOT EXISTS (
                    SELECT 1 FROM forms."FormVersions" v WHERE v."FormId" = f."Id"
                );
                """);

            migrationBuilder.Sql(
                """
                UPDATE forms."Forms" f
                SET "DraftVersionId" = v."Id",
                    "PublishedVersionId" = CASE WHEN f."Status" = 1 THEN v."Id" ELSE f."PublishedVersionId" END
                FROM forms."FormVersions" v
                WHERE v."FormId" = f."Id"
                  AND v."VersionNumber" = 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE forms."Submissions" s
                SET "FormVersionId" = COALESCE(f."PublishedVersionId", f."DraftVersionId")
                FROM forms."Forms" f
                WHERE s."FormId" = f."Id"
                  AND s."FormVersionId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "FormVersionId",
                schema: "forms",
                table: "Submissions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_FormVersionId",
                schema: "forms",
                table: "Submissions",
                column: "FormVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_DraftVersionId",
                schema: "forms",
                table: "Forms",
                column: "DraftVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_Key",
                schema: "forms",
                table: "Forms",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Forms_PublishedVersionId",
                schema: "forms",
                table: "Forms",
                column: "PublishedVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_FormId",
                schema: "forms",
                table: "FormVersions",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_FormId_VersionNumber",
                schema: "forms",
                table: "FormVersions",
                columns: new[] { "FormId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_State",
                schema: "forms",
                table: "FormVersions",
                column: "State");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_FormVersions_FormVersionId",
                schema: "forms",
                table: "Submissions",
                column: "FormVersionId",
                principalSchema: "forms",
                principalTable: "FormVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_FormVersions_FormVersionId",
                schema: "forms",
                table: "Submissions");

            migrationBuilder.DropTable(
                name: "FormVersions",
                schema: "forms");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_FormVersionId",
                schema: "forms",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Forms_DraftVersionId",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropIndex(
                name: "IX_Forms_Key",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropIndex(
                name: "IX_Forms_PublishedVersionId",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "FormVersionId",
                schema: "forms",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "DraftVersionId",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "Key",
                schema: "forms",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "PublishedVersionId",
                schema: "forms",
                table: "Forms");
        }
    }
}

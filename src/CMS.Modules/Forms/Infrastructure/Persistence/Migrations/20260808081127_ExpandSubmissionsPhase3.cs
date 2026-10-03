using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Forms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandSubmissionsPhase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContextJson",
                schema: "forms",
                table: "Submissions",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "DataJson",
                schema: "forms",
                table: "Submissions",
                type: "jsonb",
                nullable: false,
                defaultValueSql: """'{"fields":[]}'::jsonb""");

            migrationBuilder.CreateTable(
                name: "SubmissionFiles",
                schema: "forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PublicUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionFiles_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalSchema: "forms",
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionFiles_FieldId",
                schema: "forms",
                table: "SubmissionFiles",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionFiles_FieldKey",
                schema: "forms",
                table: "SubmissionFiles",
                column: "FieldKey");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionFiles_SubmissionId",
                schema: "forms",
                table: "SubmissionFiles",
                column: "SubmissionId");

            // Best-effort backfill DataJson from SubmissionValues (Values table kept).
            migrationBuilder.Sql(
                """
                UPDATE forms."Submissions" s
                SET "DataJson" = COALESCE((
                    SELECT jsonb_build_object(
                        'fields',
                        COALESCE(jsonb_agg(
                            jsonb_build_object(
                                'fieldId', CASE WHEN v."FieldId" IS NULL THEN NULL ELSE v."FieldId"::text END,
                                'key', v."FieldKey",
                                'label', v."FieldLabel",
                                'value', v."Value"
                            )
                            ORDER BY v."CreatedAtUtc"
                        ), '[]'::jsonb)
                    )
                    FROM forms."SubmissionValues" v
                    WHERE v."SubmissionId" = s."Id"
                ), '{"fields":[]}'::jsonb)
                WHERE EXISTS (
                    SELECT 1 FROM forms."SubmissionValues" v WHERE v."SubmissionId" = s."Id"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionFiles",
                schema: "forms");

            migrationBuilder.DropColumn(
                name: "ContextJson",
                schema: "forms",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "DataJson",
                schema: "forms",
                table: "Submissions");
        }
    }
}

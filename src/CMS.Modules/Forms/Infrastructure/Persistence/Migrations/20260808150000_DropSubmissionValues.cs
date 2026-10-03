using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Modules.Forms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropSubmissionValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill DataJson from SubmissionValues where still at empty default.
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
                WHERE s."DataJson" = '{"fields":[]}'::jsonb
                  AND EXISTS (
                    SELECT 1 FROM forms."SubmissionValues" v WHERE v."SubmissionId" = s."Id"
                  );
                """);

            migrationBuilder.DropTable(
                name: "SubmissionValues",
                schema: "forms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubmissionValues",
                schema: "forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FieldLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionValues_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalSchema: "forms",
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionValues_FieldId",
                schema: "forms",
                table: "SubmissionValues",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionValues_SubmissionId",
                schema: "forms",
                table: "SubmissionValues",
                column: "SubmissionId");
        }
    }
}

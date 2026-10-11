using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CMS.Modules.Comments.Infrastructure.Persistence;

#nullable disable

namespace CMS.Modules.Comments.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommentsDbContext))]
[Migration("20261011120000_AddCommentImportSourceFields")]
public class AddCommentImportSourceFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ParentId",
            schema: "comments",
            table: "Comments",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SourceSystem",
            schema: "comments",
            table: "Comments",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SourceExternalId",
            schema: "comments",
            table: "Comments",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SourcePageUrl",
            schema: "comments",
            table: "Comments",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Comments_ParentId",
            schema: "comments",
            table: "Comments",
            column: "ParentId");

        migrationBuilder.CreateIndex(
            name: "IX_Comments_SourceSystem_SourceExternalId",
            schema: "comments",
            table: "Comments",
            columns: new[] { "SourceSystem", "SourceExternalId" },
            unique: true,
            filter: "\"SourceSystem\" IS NOT NULL AND \"SourceExternalId\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Comments_SourceSystem_SourceExternalId",
            schema: "comments",
            table: "Comments");

        migrationBuilder.DropIndex(
            name: "IX_Comments_ParentId",
            schema: "comments",
            table: "Comments");

        migrationBuilder.DropColumn(
            name: "SourcePageUrl",
            schema: "comments",
            table: "Comments");

        migrationBuilder.DropColumn(
            name: "SourceExternalId",
            schema: "comments",
            table: "Comments");

        migrationBuilder.DropColumn(
            name: "SourceSystem",
            schema: "comments",
            table: "Comments");

        migrationBuilder.DropColumn(
            name: "ParentId",
            schema: "comments",
            table: "Comments");
    }
}

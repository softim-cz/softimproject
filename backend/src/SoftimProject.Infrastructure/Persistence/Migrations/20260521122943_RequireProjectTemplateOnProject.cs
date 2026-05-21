using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequireProjectTemplateOnProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectTemplates_ProjectTemplateId",
                table: "Projects");

            // Backfill projektů bez šablony → Default template
            // (Ids.DefaultTemplate v DatabaseSeeder). Bez tohoto kroku by
            // AlterColumn(NOT NULL) zachoval Guid.Empty u řádků s NULL
            // a FK na ProjectTemplates by selhal.
            migrationBuilder.Sql(@"
                UPDATE Projects
                SET ProjectTemplateId = '00000000-0000-0000-0000-000000000001'
                WHERE ProjectTemplateId IS NULL;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectTemplateId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectTemplates_ProjectTemplateId",
                table: "Projects",
                column: "ProjectTemplateId",
                principalTable: "ProjectTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectTemplates_ProjectTemplateId",
                table: "Projects");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectTemplateId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectTemplates_ProjectTemplateId",
                table: "Projects",
                column: "ProjectTemplateId",
                principalTable: "ProjectTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

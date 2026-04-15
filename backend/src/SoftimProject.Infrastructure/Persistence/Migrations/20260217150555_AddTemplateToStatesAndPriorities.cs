using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateToStatesAndPriorities : Migration
    {
        private static readonly Guid DefaultTemplateId = new("00000000-0000-0000-0000-000000000001");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Ensure a "Default" template exists
            migrationBuilder.Sql($"""
                IF NOT EXISTS (SELECT 1 FROM ProjectTemplates WHERE Id = '{DefaultTemplateId}')
                BEGIN
                    INSERT INTO ProjectTemplates (Id, Name, Description, IsActive, CreatedAt)
                    VALUES ('{DefaultTemplateId}', N'Default', N'Default project template', 1, GETUTCDATE());
                END
                """);

            // 2. Drop old unique indexes
            migrationBuilder.DropIndex(
                name: "IX_TicketPriorities_Name",
                table: "TicketPriorities");

            migrationBuilder.DropIndex(
                name: "IX_TaskStates_Name",
                table: "TaskStates");

            // 3. Add columns as nullable first
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectTemplateId",
                table: "TaskStates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectTemplateId",
                table: "TicketPriorities",
                type: "uniqueidentifier",
                nullable: true);

            // 4. Assign all existing records to the Default template
            migrationBuilder.Sql($"""
                UPDATE TaskStates SET ProjectTemplateId = '{DefaultTemplateId}' WHERE ProjectTemplateId IS NULL;
                UPDATE TicketPriorities SET ProjectTemplateId = '{DefaultTemplateId}' WHERE ProjectTemplateId IS NULL;
                """);

            // 5. Also assign unlinked projects to the Default template
            migrationBuilder.Sql($"""
                UPDATE Projects SET ProjectTemplateId = '{DefaultTemplateId}' WHERE ProjectTemplateId IS NULL;
                """);

            // 6. Make columns NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectTemplateId",
                table: "TaskStates",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: DefaultTemplateId);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectTemplateId",
                table: "TicketPriorities",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: DefaultTemplateId);

            // 7. Create composite unique indexes
            migrationBuilder.CreateIndex(
                name: "IX_TaskStates_ProjectTemplateId_Name",
                table: "TaskStates",
                columns: new[] { "ProjectTemplateId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorities_ProjectTemplateId_Name",
                table: "TicketPriorities",
                columns: new[] { "ProjectTemplateId", "Name" },
                unique: true);

            // 8. Add FK constraints
            migrationBuilder.AddForeignKey(
                name: "FK_TaskStates_ProjectTemplates_ProjectTemplateId",
                table: "TaskStates",
                column: "ProjectTemplateId",
                principalTable: "ProjectTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketPriorities_ProjectTemplates_ProjectTemplateId",
                table: "TicketPriorities",
                column: "ProjectTemplateId",
                principalTable: "ProjectTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskStates_ProjectTemplates_ProjectTemplateId",
                table: "TaskStates");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketPriorities_ProjectTemplates_ProjectTemplateId",
                table: "TicketPriorities");

            migrationBuilder.DropIndex(
                name: "IX_TicketPriorities_ProjectTemplateId_Name",
                table: "TicketPriorities");

            migrationBuilder.DropIndex(
                name: "IX_TaskStates_ProjectTemplateId_Name",
                table: "TaskStates");

            migrationBuilder.DropColumn(
                name: "ProjectTemplateId",
                table: "TicketPriorities");

            migrationBuilder.DropColumn(
                name: "ProjectTemplateId",
                table: "TaskStates");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorities_Name",
                table: "TicketPriorities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskStates_Name",
                table: "TaskStates",
                column: "Name",
                unique: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketNumberAndProjectNextTicketNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add Ticket.Number
            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 2. Add Project.NextTicketNumber
            migrationBuilder.AddColumn<int>(
                name: "NextTicketNumber",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // 3. Populate ticket numbers per project
            migrationBuilder.Sql(@"
                WITH numbered AS (
                    SELECT Id, ProjectId, ROW_NUMBER() OVER (PARTITION BY ProjectId ORDER BY CreatedAt) AS Num
                    FROM Tickets
                )
                UPDATE t SET t.Number = n.Num
                FROM Tickets t INNER JOIN numbered n ON t.Id = n.Id;

                UPDATE p SET p.NextTicketNumber = ISNULL((SELECT MAX(Number) + 1 FROM Tickets WHERE ProjectId = p.Id), 1)
                FROM Projects p;
            ");

            // 4. Add unique index on (ProjectId, Number)
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ProjectId_Number",
                table: "Tickets",
                columns: new[] { "ProjectId", "Number" },
                unique: true);

            // 5. Add KanbanColumns.Color
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "KanbanColumns",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true);

            // 6. Create M:M junction table
            migrationBuilder.CreateTable(
                name: "KanbanColumnTaskState",
                columns: table => new
                {
                    KanbanColumnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskStateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanColumnTaskState", x => new { x.KanbanColumnId, x.TaskStateId });
                    table.ForeignKey(
                        name: "FK_KanbanColumnTaskState_KanbanColumns_KanbanColumnId",
                        column: x => x.KanbanColumnId,
                        principalTable: "KanbanColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KanbanColumnTaskState_TaskStates_TaskStateId",
                        column: x => x.TaskStateId,
                        principalTable: "TaskStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KanbanColumnTaskState_TaskStateId",
                table: "KanbanColumnTaskState",
                column: "TaskStateId");

            // 7. Migrate existing MapsToTaskStateId data into junction table
            migrationBuilder.Sql(@"
                INSERT INTO KanbanColumnTaskState (KanbanColumnId, TaskStateId)
                SELECT Id, MapsToTaskStateId FROM KanbanColumns WHERE MapsToTaskStateId IS NOT NULL;
            ");

            // 8. Drop old FK, index, and column
            migrationBuilder.DropForeignKey(
                name: "FK_KanbanColumns_TaskStates_MapsToTaskStateId",
                table: "KanbanColumns");

            migrationBuilder.DropIndex(
                name: "IX_KanbanColumns_MapsToTaskStateId",
                table: "KanbanColumns");

            migrationBuilder.DropColumn(
                name: "MapsToTaskStateId",
                table: "KanbanColumns");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MapsToTaskStateId",
                table: "KanbanColumns",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Restore first mapped state from junction table
            migrationBuilder.Sql(@"
                UPDATE kc SET kc.MapsToTaskStateId = (
                    SELECT TOP 1 TaskStateId FROM KanbanColumnTaskState WHERE KanbanColumnId = kc.Id
                )
                FROM KanbanColumns kc;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanColumns_MapsToTaskStateId",
                table: "KanbanColumns",
                column: "MapsToTaskStateId");

            migrationBuilder.AddForeignKey(
                name: "FK_KanbanColumns_TaskStates_MapsToTaskStateId",
                table: "KanbanColumns",
                column: "MapsToTaskStateId",
                principalTable: "TaskStates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "KanbanColumnTaskState");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ProjectId_Number",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "NextTicketNumber",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "KanbanColumns");
        }
    }
}

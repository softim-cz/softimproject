using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertEnumsToLookupTables : Migration
    {
        // Fixed GUIDs for seed data
        private static readonly Guid PriorityLowId = new("10000000-0000-0000-0000-000000000001");
        private static readonly Guid PriorityMediumId = new("10000000-0000-0000-0000-000000000002");
        private static readonly Guid PriorityHighId = new("10000000-0000-0000-0000-000000000003");
        private static readonly Guid PriorityCriticalId = new("10000000-0000-0000-0000-000000000004");

        private static readonly Guid StateBacklogId = new("20000000-0000-0000-0000-000000000001");
        private static readonly Guid StateTodoId = new("20000000-0000-0000-0000-000000000002");
        private static readonly Guid StateInProgressId = new("20000000-0000-0000-0000-000000000003");
        private static readonly Guid StateReviewId = new("20000000-0000-0000-0000-000000000004");
        private static readonly Guid StateDoneId = new("20000000-0000-0000-0000-000000000005");
        private static readonly Guid StateClosedId = new("20000000-0000-0000-0000-000000000006");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create TicketPriorities table
            migrationBuilder.CreateTable(
                name: "TicketPriorities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketPriorities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorities_Name",
                table: "TicketPriorities",
                column: "Name",
                unique: true);

            // 2. Seed TicketPriorities
            var now = DateTime.UtcNow;
            migrationBuilder.Sql($@"
                INSERT INTO TicketPriorities (Id, Name, Color, SortOrder, IsActive, IsDefault, CreatedAt)
                VALUES
                    ('{PriorityLowId}',      'Low',      '#22c55e', 10, 1, 0, '{now:yyyy-MM-ddTHH:mm:ss}'),
                    ('{PriorityMediumId}',   'Medium',   '#f59e0b', 20, 1, 1, '{now:yyyy-MM-ddTHH:mm:ss}'),
                    ('{PriorityHighId}',     'High',     '#ef4444', 30, 1, 0, '{now:yyyy-MM-ddTHH:mm:ss}'),
                    ('{PriorityCriticalId}', 'Critical', '#991b1b', 40, 1, 0, '{now:yyyy-MM-ddTHH:mm:ss}');
            ");

            // 3. Seed default TaskStates if they don't exist
            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM TaskStates WHERE Name = 'Backlog')
                    INSERT INTO TaskStates (Id, Name, Color, SortOrder, IsActive, IsDefault, IsClosedState, CreatedAt)
                    VALUES ('{StateBacklogId}', 'Backlog', '#94a3b8', 10, 1, 1, 0, '{now:yyyy-MM-ddTHH:mm:ss}');

                IF NOT EXISTS (SELECT 1 FROM TaskStates WHERE Name = 'Todo')
                    INSERT INTO TaskStates (Id, Name, Color, SortOrder, IsActive, IsDefault, IsClosedState, CreatedAt)
                    VALUES ('{StateTodoId}', 'Todo', '#3b82f6', 20, 1, 0, 0, '{now:yyyy-MM-ddTHH:mm:ss}');

                IF NOT EXISTS (SELECT 1 FROM TaskStates WHERE Name = 'InProgress')
                    INSERT INTO TaskStates (Id, Name, Color, SortOrder, IsActive, IsDefault, IsClosedState, CreatedAt)
                    VALUES ('{StateInProgressId}', 'InProgress', '#f59e0b', 30, 1, 0, 0, '{now:yyyy-MM-ddTHH:mm:ss}');

                IF NOT EXISTS (SELECT 1 FROM TaskStates WHERE Name = 'Review')
                    INSERT INTO TaskStates (Id, Name, Color, SortOrder, IsActive, IsDefault, IsClosedState, CreatedAt)
                    VALUES ('{StateReviewId}', 'Review', '#8b5cf6', 40, 1, 0, 0, '{now:yyyy-MM-ddTHH:mm:ss}');

                IF NOT EXISTS (SELECT 1 FROM TaskStates WHERE Name = 'Done')
                    INSERT INTO TaskStates (Id, Name, Color, SortOrder, IsActive, IsDefault, IsClosedState, CreatedAt)
                    VALUES ('{StateDoneId}', 'Done', '#22c55e', 50, 1, 0, 1, '{now:yyyy-MM-ddTHH:mm:ss}');

                IF NOT EXISTS (SELECT 1 FROM TaskStates WHERE Name = 'Closed')
                    INSERT INTO TaskStates (Id, Name, Color, SortOrder, IsActive, IsDefault, IsClosedState, CreatedAt)
                    VALUES ('{StateClosedId}', 'Closed', '#64748b', 60, 1, 0, 1, '{now:yyyy-MM-ddTHH:mm:ss}');
            ");

            // 4. Add TicketPriorityId as NULLABLE first
            migrationBuilder.AddColumn<Guid>(
                name: "TicketPriorityId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            // 5. Populate TicketPriorityId from Priority string
            migrationBuilder.Sql($@"
                UPDATE t SET t.TicketPriorityId = p.Id
                FROM Tickets t
                INNER JOIN TicketPriorities p ON p.Name = t.Priority;

                -- Fallback: any unmapped priorities get Medium
                UPDATE Tickets SET TicketPriorityId = '{PriorityMediumId}'
                WHERE TicketPriorityId IS NULL;
            ");

            // 6. Make TicketPriorityId NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "TicketPriorityId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // 7. Drop Priority column and old index
            migrationBuilder.DropIndex(
                name: "IX_Tickets_ProjectId_Status",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Tickets");

            // 8. Populate TaskStateId from Status string (where NULL)
            migrationBuilder.Sql($@"
                UPDATE t SET t.TaskStateId = ts.Id
                FROM Tickets t
                INNER JOIN TaskStates ts ON ts.Name = t.Status
                WHERE t.TaskStateId IS NULL;

                -- Fallback: any remaining NULLs get the default TaskState
                UPDATE Tickets SET TaskStateId = (
                    SELECT TOP 1 Id FROM TaskStates WHERE IsDefault = 1 AND IsActive = 1
                    ORDER BY SortOrder
                )
                WHERE TaskStateId IS NULL;
            ");

            // 9. Make TaskStateId NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "TaskStateId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // 10. Drop Status column
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tickets");

            // 11. Populate KanbanColumns.MapsToTaskStateId from MapsToStatus (where NULL)
            migrationBuilder.Sql($@"
                UPDATE kc SET kc.MapsToTaskStateId = ts.Id
                FROM KanbanColumns kc
                INNER JOIN TaskStates ts ON ts.Name = kc.MapsToStatus
                WHERE kc.MapsToTaskStateId IS NULL;

                -- Fallback: any remaining NULLs get the default TaskState
                UPDATE KanbanColumns SET MapsToTaskStateId = (
                    SELECT TOP 1 Id FROM TaskStates WHERE IsDefault = 1 AND IsActive = 1
                    ORDER BY SortOrder
                )
                WHERE MapsToTaskStateId IS NULL;
            ");

            // 12. Make MapsToTaskStateId NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "MapsToTaskStateId",
                table: "KanbanColumns",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // 13. Drop MapsToStatus column
            migrationBuilder.DropColumn(
                name: "MapsToStatus",
                table: "KanbanColumns");

            // 14. Create new indexes and FK
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ProjectId_TaskStateId",
                table: "Tickets",
                columns: new[] { "ProjectId", "TaskStateId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketPriorityId",
                table: "Tickets",
                column: "TicketPriorityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_TicketPriorities_TicketPriorityId",
                table: "Tickets",
                column: "TicketPriorityId",
                principalTable: "TicketPriorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_TicketPriorities_TicketPriorityId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ProjectId_TaskStateId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_TicketPriorityId",
                table: "Tickets");

            // Restore Status column
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Tickets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Backlog");

            // Restore Priority column
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Tickets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Medium");

            // Populate from lookup tables
            migrationBuilder.Sql(@"
                UPDATE t SET t.Status = ts.Name
                FROM Tickets t
                INNER JOIN TaskStates ts ON ts.Id = t.TaskStateId;

                UPDATE t SET t.Priority = p.Name
                FROM Tickets t
                INNER JOIN TicketPriorities p ON p.Id = t.TicketPriorityId;
            ");

            // Revert TaskStateId to nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "TaskStateId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.DropColumn(
                name: "TicketPriorityId",
                table: "Tickets");

            // Restore MapsToStatus
            migrationBuilder.AddColumn<string>(
                name: "MapsToStatus",
                table: "KanbanColumns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE kc SET kc.MapsToStatus = ts.Name
                FROM KanbanColumns kc
                INNER JOIN TaskStates ts ON ts.Id = kc.MapsToTaskStateId;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "MapsToTaskStateId",
                table: "KanbanColumns",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.DropTable(
                name: "TicketPriorities");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ProjectId_Status",
                table: "Tickets",
                columns: new[] { "ProjectId", "Status" });
        }
    }
}

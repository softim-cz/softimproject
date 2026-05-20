using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBilingualLookupNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameCs",
                table: "TicketPriorities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "TicketPriorities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameCs",
                table: "TaskTypes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "TaskTypes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameCs",
                table: "TaskStates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "TaskStates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameCs",
                table: "ProjectTypes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "ProjectTypes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameCs",
                table: "ProjectStates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "ProjectStates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameCs",
                table: "ApplicationRoles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "ApplicationRoles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Backfill bilingual names for seeded lookup values.
            // CS = český překlad, EN = původní anglický název. Pro custom (user-created)
            // hodnoty zůstanou NameCs/NameEn null a UI padá zpět na Name.
            migrationBuilder.Sql(@"
                UPDATE TicketPriorities SET NameCs = N'Nízká',     NameEn = N'Low'      WHERE Name = 'Low';
                UPDATE TicketPriorities SET NameCs = N'Střední',   NameEn = N'Medium'   WHERE Name = 'Medium';
                UPDATE TicketPriorities SET NameCs = N'Vysoká',    NameEn = N'High'     WHERE Name = 'High';
                UPDATE TicketPriorities SET NameCs = N'Kritická',  NameEn = N'Critical' WHERE Name = 'Critical';

                UPDATE TaskStates SET NameCs = N'Backlog',     NameEn = N'Backlog'     WHERE Name = 'Backlog';
                UPDATE TaskStates SET NameCs = N'K řešení',    NameEn = N'Todo'        WHERE Name = 'Todo';
                UPDATE TaskStates SET NameCs = N'Probíhá',     NameEn = N'In Progress' WHERE Name IN ('InProgress', 'In Progress');
                UPDATE TaskStates SET NameCs = N'Ke kontrole', NameEn = N'Review'      WHERE Name = 'Review';
                UPDATE TaskStates SET NameCs = N'Hotovo',      NameEn = N'Done'        WHERE Name = 'Done';
                UPDATE TaskStates SET NameCs = N'Uzavřeno',    NameEn = N'Closed'      WHERE Name = 'Closed';

                UPDATE ProjectStates SET NameCs = N'Aktivní',    NameEn = N'Active'    WHERE Name = 'Active';
                UPDATE ProjectStates SET NameCs = N'Pozastaven', NameEn = N'On Hold'   WHERE Name = 'OnHold';
                UPDATE ProjectStates SET NameCs = N'Dokončený',  NameEn = N'Completed' WHERE Name = 'Completed';
                UPDATE ProjectStates SET NameCs = N'Archivován', NameEn = N'Archived'  WHERE Name = 'Archived';

                UPDATE ProjectTypes SET NameCs = N'Vývoj', NameEn = N'Development' WHERE Name = 'Development';

                UPDATE TaskTypes SET NameCs = N'Funkce', NameEn = N'Feature' WHERE Name = 'Feature';
                UPDATE TaskTypes SET NameCs = N'Chyba',  NameEn = N'Bug'     WHERE Name = 'Bug';
                UPDATE TaskTypes SET NameCs = N'Úkol',   NameEn = N'Task'    WHERE Name = 'Task';

                UPDATE ApplicationRoles SET NameCs = N'Administrátor', NameEn = N'Admin'    WHERE Name = 'Admin';
                UPDATE ApplicationRoles SET NameCs = N'Manažer',       NameEn = N'Manager'  WHERE Name = 'Manager';
                UPDATE ApplicationRoles SET NameCs = N'Uživatel',      NameEn = N'User'     WHERE Name = 'User';
                UPDATE ApplicationRoles SET NameCs = N'Externí',       NameEn = N'External' WHERE Name = 'External';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameCs",
                table: "TicketPriorities");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "TicketPriorities");

            migrationBuilder.DropColumn(
                name: "NameCs",
                table: "TaskTypes");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "TaskTypes");

            migrationBuilder.DropColumn(
                name: "NameCs",
                table: "TaskStates");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "TaskStates");

            migrationBuilder.DropColumn(
                name: "NameCs",
                table: "ProjectTypes");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "ProjectTypes");

            migrationBuilder.DropColumn(
                name: "NameCs",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "ProjectStates");

            migrationBuilder.DropColumn(
                name: "NameCs",
                table: "ApplicationRoles");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "ApplicationRoles");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCeoRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Worklogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Invoiced",
                table: "Worklogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorporateRole",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CumulativeWorkedHours",
                table: "Tickets",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExternalBudget",
                table: "Tickets",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalUser",
                table: "Tickets",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImplementationNotes",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastComment",
                table: "Tickets",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentTicketId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskStateId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskTypeId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentProjectId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectStateId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectTypeId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MapsToTaskStateId",
                table: "KanbanColumns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TicketId",
                table: "Comments",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ExternalUser",
                table: "Comments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Comments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicationRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ProjectsCreate = table.Column<bool>(type: "bit", nullable: false),
                    ProjectsRead = table.Column<bool>(type: "bit", nullable: false),
                    ProjectsUpdate = table.Column<bool>(type: "bit", nullable: false),
                    ProjectsDelete = table.Column<bool>(type: "bit", nullable: false),
                    TimeTrackingCreate = table.Column<bool>(type: "bit", nullable: false),
                    TimeTrackingRead = table.Column<bool>(type: "bit", nullable: false),
                    TimeTrackingUpdate = table.Column<bool>(type: "bit", nullable: false),
                    TimeTrackingDelete = table.Column<bool>(type: "bit", nullable: false),
                    ReportsCreate = table.Column<bool>(type: "bit", nullable: false),
                    ReportsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReportsUpdate = table.Column<bool>(type: "bit", nullable: false),
                    ReportsDelete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectStates",
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
                    table.PrimaryKey("PK_ProjectStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedFilters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ViewType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FilterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedFilters_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SavedFilters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsClosedState = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ViewConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ViewType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViewConfigurations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ViewConfigurations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserApplicationRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserApplicationRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserApplicationRoles_ApplicationRoles_ApplicationRoleId",
                        column: x => x.ApplicationRoleId,
                        principalTable: "ApplicationRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserApplicationRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ParentTicketId",
                table: "Tickets",
                column: "ParentTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TaskStateId",
                table: "Tickets",
                column: "TaskStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TaskTypeId",
                table: "Tickets",
                column: "TaskTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CompanyId",
                table: "Projects",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ParentProjectId",
                table: "Projects",
                column: "ParentProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectStateId",
                table: "Projects",
                column: "ProjectStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectTypeId",
                table: "Projects",
                column: "ProjectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanColumns_MapsToTaskStateId",
                table: "KanbanColumns",
                column: "MapsToTaskStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ProjectId",
                table: "Comments",
                column: "ProjectId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Comment_TicketOrProject",
                table: "Comments",
                sql: "[TicketId] IS NOT NULL OR [ProjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRoles_Name",
                table: "ApplicationRoles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStates_Name",
                table: "ProjectStates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTypes_Name",
                table: "ProjectTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedFilters_ProjectId",
                table: "SavedFilters",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedFilters_UserId",
                table: "SavedFilters",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskStates_Name",
                table: "TaskStates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskTypes_Name",
                table: "TaskTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserApplicationRoles_ApplicationRoleId",
                table: "UserApplicationRoles",
                column: "ApplicationRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserApplicationRoles_UserId_ApplicationRoleId",
                table: "UserApplicationRoles",
                columns: new[] { "UserId", "ApplicationRoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ViewConfigurations_ProjectId",
                table: "ViewConfigurations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewConfigurations_UserId_ProjectId_ViewType",
                table: "ViewConfigurations",
                columns: new[] { "UserId", "ProjectId", "ViewType" },
                unique: true,
                filter: "[ProjectId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Projects_ProjectId",
                table: "Comments",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KanbanColumns_TaskStates_MapsToTaskStateId",
                table: "KanbanColumns",
                column: "MapsToTaskStateId",
                principalTable: "TaskStates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Companies_CompanyId",
                table: "Projects",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectStates_ProjectStateId",
                table: "Projects",
                column: "ProjectStateId",
                principalTable: "ProjectStates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectTypes_ProjectTypeId",
                table: "Projects",
                column: "ProjectTypeId",
                principalTable: "ProjectTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Projects_ParentProjectId",
                table: "Projects",
                column: "ParentProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_TaskStates_TaskStateId",
                table: "Tickets",
                column: "TaskStateId",
                principalTable: "TaskStates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_TaskTypes_TaskTypeId",
                table: "Tickets",
                column: "TaskTypeId",
                principalTable: "TaskTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Tickets_ParentTicketId",
                table: "Tickets",
                column: "ParentTicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Projects_ProjectId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_KanbanColumns_TaskStates_MapsToTaskStateId",
                table: "KanbanColumns");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Companies_CompanyId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectStates_ProjectStateId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectTypes_ProjectTypeId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Projects_ParentProjectId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_TaskStates_TaskStateId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_TaskTypes_TaskTypeId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Tickets_ParentTicketId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "ProjectStates");

            migrationBuilder.DropTable(
                name: "ProjectTypes");

            migrationBuilder.DropTable(
                name: "SavedFilters");

            migrationBuilder.DropTable(
                name: "TaskStates");

            migrationBuilder.DropTable(
                name: "TaskTypes");

            migrationBuilder.DropTable(
                name: "UserApplicationRoles");

            migrationBuilder.DropTable(
                name: "ViewConfigurations");

            migrationBuilder.DropTable(
                name: "ApplicationRoles");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ParentTicketId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_TaskStateId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_TaskTypeId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Projects_CompanyId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ParentProjectId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectStateId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectTypeId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_KanbanColumns_MapsToTaskStateId",
                table: "KanbanColumns");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ProjectId",
                table: "Comments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Comment_TicketOrProject",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Worklogs");

            migrationBuilder.DropColumn(
                name: "Invoiced",
                table: "Worklogs");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CorporateRole",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CumulativeWorkedHours",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ExternalBudget",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ExternalUser",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ImplementationNotes",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "LastComment",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ParentTicketId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TaskStateId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TaskTypeId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ParentProjectId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectStateId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectTypeId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "MapsToTaskStateId",
                table: "KanbanColumns");

            migrationBuilder.DropColumn(
                name: "ExternalUser",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Comments");

            migrationBuilder.AlterColumn<Guid>(
                name: "TicketId",
                table: "Comments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}

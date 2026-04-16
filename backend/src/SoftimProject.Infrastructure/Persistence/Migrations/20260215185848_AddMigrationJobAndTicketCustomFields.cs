using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrationJobAndTicketCustomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Worklogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "ChecklistItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppliesTo",
                table: "CustomFieldDefinitions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MigrationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitiatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceBaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProjectsTotal = table.Column<int>(type: "int", nullable: false),
                    ProjectsMigrated = table.Column<int>(type: "int", nullable: false),
                    TicketsTotal = table.Column<int>(type: "int", nullable: false),
                    TicketsMigrated = table.Column<int>(type: "int", nullable: false),
                    CommentsTotal = table.Column<int>(type: "int", nullable: false),
                    CommentsMigrated = table.Column<int>(type: "int", nullable: false),
                    WorklogsTotal = table.Column<int>(type: "int", nullable: false),
                    WorklogsMigrated = table.Column<int>(type: "int", nullable: false),
                    AttachmentsTotal = table.Column<int>(type: "int", nullable: false),
                    AttachmentsMigrated = table.Column<int>(type: "int", nullable: false),
                    ItemsFailed = table.Column<int>(type: "int", nullable: false),
                    ItemsSkipped = table.Column<int>(type: "int", nullable: false),
                    ItemsUpdated = table.Column<int>(type: "int", nullable: false),
                    ItemsCreated = table.Column<int>(type: "int", nullable: false),
                    ErrorLog = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Configuration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MigrationJobs_Users_InitiatedByUserId",
                        column: x => x.InitiatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TicketCustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomFieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketCustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketCustomFieldValues_CustomFieldDefinitions_CustomFieldDefinitionId",
                        column: x => x.CustomFieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketCustomFieldValues_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MigrationJobs_InitiatedByUserId",
                table: "MigrationJobs",
                column: "InitiatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCustomFieldValues_CustomFieldDefinitionId",
                table: "TicketCustomFieldValues",
                column: "CustomFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCustomFieldValues_TicketId_CustomFieldDefinitionId",
                table: "TicketCustomFieldValues",
                columns: new[] { "TicketId", "CustomFieldDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MigrationJobs");

            migrationBuilder.DropTable(
                name: "TicketCustomFieldValues");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Worklogs");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "ChecklistItems");

            migrationBuilder.DropColumn(
                name: "AppliesTo",
                table: "CustomFieldDefinitions");
        }
    }
}

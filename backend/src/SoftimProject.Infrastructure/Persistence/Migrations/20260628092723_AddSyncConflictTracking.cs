using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncConflictTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_ExternalId",
                table: "Tickets");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedFromSourceAt",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Worklogs_ExternalId",
                table: "Worklogs",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ProjectId_ExternalId",
                table: "Tickets",
                columns: new[] { "ProjectId", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ExternalSystem_ExternalProjectId",
                table: "Projects",
                columns: new[] { "ExternalSystem", "ExternalProjectId" },
                unique: true,
                filter: "[ExternalProjectId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Worklogs_ExternalId",
                table: "Worklogs");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ProjectId_ExternalId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ExternalSystem_ExternalProjectId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "LastSyncedFromSourceAt",
                table: "Tickets");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ExternalId",
                table: "Tickets",
                column: "ExternalId");
        }
    }
}

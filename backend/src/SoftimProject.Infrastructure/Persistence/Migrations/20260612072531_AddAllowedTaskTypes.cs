using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowedTaskTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectAllowedTaskTypes",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAllowedTaskTypes", x => new { x.ProjectId, x.TaskTypeId });
                    table.ForeignKey(
                        name: "FK_ProjectAllowedTaskTypes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectAllowedTaskTypes_TaskTypes_TaskTypeId",
                        column: x => x.TaskTypeId,
                        principalTable: "TaskTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTemplateAllowedTaskTypes",
                columns: table => new
                {
                    ProjectTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTemplateAllowedTaskTypes", x => new { x.ProjectTemplateId, x.TaskTypeId });
                    table.ForeignKey(
                        name: "FK_ProjectTemplateAllowedTaskTypes_ProjectTemplates_ProjectTemplateId",
                        column: x => x.ProjectTemplateId,
                        principalTable: "ProjectTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTemplateAllowedTaskTypes_TaskTypes_TaskTypeId",
                        column: x => x.TaskTypeId,
                        principalTable: "TaskTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAllowedTaskTypes_TaskTypeId",
                table: "ProjectAllowedTaskTypes",
                column: "TaskTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTemplateAllowedTaskTypes_TaskTypeId",
                table: "ProjectTemplateAllowedTaskTypes",
                column: "TaskTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectAllowedTaskTypes");

            migrationBuilder.DropTable(
                name: "ProjectTemplateAllowedTaskTypes");
        }
    }
}

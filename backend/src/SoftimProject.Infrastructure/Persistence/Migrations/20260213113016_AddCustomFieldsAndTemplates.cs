using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomFieldsAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectTemplateId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FieldType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTemplates",
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
                    table.PrimaryKey("PK_ProjectTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectCustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomFieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectCustomFieldValues_CustomFieldDefinitions_CustomFieldDefinitionId",
                        column: x => x.CustomFieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectCustomFieldValues_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTemplateFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomFieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTemplateFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTemplateFields_CustomFieldDefinitions_CustomFieldDefinitionId",
                        column: x => x.CustomFieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTemplateFields_ProjectTemplates_ProjectTemplateId",
                        column: x => x.ProjectTemplateId,
                        principalTable: "ProjectTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectTemplateId",
                table: "Projects",
                column: "ProjectTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_Name",
                table: "CustomFieldDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCustomFieldValues_CustomFieldDefinitionId",
                table: "ProjectCustomFieldValues",
                column: "CustomFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCustomFieldValues_ProjectId_CustomFieldDefinitionId",
                table: "ProjectCustomFieldValues",
                columns: new[] { "ProjectId", "CustomFieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTemplateFields_CustomFieldDefinitionId",
                table: "ProjectTemplateFields",
                column: "CustomFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTemplateFields_ProjectTemplateId_CustomFieldDefinitionId",
                table: "ProjectTemplateFields",
                columns: new[] { "ProjectTemplateId", "CustomFieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTemplates_Name",
                table: "ProjectTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectTemplates_ProjectTemplateId",
                table: "Projects",
                column: "ProjectTemplateId",
                principalTable: "ProjectTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectTemplates_ProjectTemplateId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "ProjectCustomFieldValues");

            migrationBuilder.DropTable(
                name: "ProjectTemplateFields");

            migrationBuilder.DropTable(
                name: "CustomFieldDefinitions");

            migrationBuilder.DropTable(
                name: "ProjectTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectTemplateId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectTemplateId",
                table: "Projects");
        }
    }
}

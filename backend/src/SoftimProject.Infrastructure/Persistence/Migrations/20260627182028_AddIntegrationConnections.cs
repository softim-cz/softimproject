using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IntegrationConnectionId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IntegrationConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    EncryptedApiToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebhookSecret = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TargetProjectTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConflictPolicy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncWatermark = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MappingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectSelectorJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_Companies_TargetCompanyId",
                        column: x => x.TargetCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_ProjectTemplates_TargetProjectTemplateId",
                        column: x => x.TargetProjectTemplateId,
                        principalTable: "ProjectTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_IntegrationConnectionId",
                table: "Projects",
                column: "IntegrationConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_SourceSystem_IsEnabled",
                table: "IntegrationConnections",
                columns: new[] { "SourceSystem", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_TargetCompanyId",
                table: "IntegrationConnections",
                column: "TargetCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_TargetProjectTemplateId",
                table: "IntegrationConnections",
                column: "TargetProjectTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_IntegrationConnections_IntegrationConnectionId",
                table: "Projects",
                column: "IntegrationConnectionId",
                principalTable: "IntegrationConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_IntegrationConnections_IntegrationConnectionId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "IntegrationConnections");

            migrationBuilder.DropIndex(
                name: "IX_Projects_IntegrationConnectionId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IntegrationConnectionId",
                table: "Projects");
        }
    }
}

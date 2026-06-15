using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrEnrichmentAndWebhookIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChecksStatus",
                table: "LinkedPullRequests",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommitsCount",
                table: "LinkedPullRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "LinkedPullRequests",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcessedWebhookDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DeliveryId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedWebhookDeliveries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedWebhookDeliveries_Provider_DeliveryId",
                table: "ProcessedWebhookDeliveries",
                columns: new[] { "Provider", "DeliveryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedWebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "ChecksStatus",
                table: "LinkedPullRequests");

            migrationBuilder.DropColumn(
                name: "CommitsCount",
                table: "LinkedPullRequests");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "LinkedPullRequests");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeadLetterEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeadLetterEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OperationKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FirstFailedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastFailedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadLetterEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeadLetterEntries_OperationType_OperationKey",
                table: "DeadLetterEntries",
                columns: new[] { "OperationType", "OperationKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeadLetterEntries_Status_LastFailedAt",
                table: "DeadLetterEntries",
                columns: new[] { "Status", "LastFailedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeadLetterEntries");
        }
    }
}

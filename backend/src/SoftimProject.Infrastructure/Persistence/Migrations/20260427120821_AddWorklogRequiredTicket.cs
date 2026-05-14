using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorklogRequiredTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Backfill: distribute worklogs with NULL TicketId evenly across
            //    existing tickets in the SAME project (round-robin by NEWID()).
            //    Worklogs in projects without any tickets are dropped — they
            //    cannot satisfy the new NOT NULL invariant. This is a test/dev
            //    safety net; in practice such rows are vanishingly rare.
            migrationBuilder.Sql(@"
;WITH RankedTickets AS (
    SELECT Id, ProjectId,
           ROW_NUMBER() OVER (PARTITION BY ProjectId ORDER BY NEWID()) - 1 AS rn,
           COUNT(*) OVER (PARTITION BY ProjectId) AS cnt
    FROM Tickets
),
RankedWorklogs AS (
    SELECT Id, ProjectId,
           ROW_NUMBER() OVER (PARTITION BY ProjectId ORDER BY CreatedAt, Id) - 1 AS rn
    FROM Worklogs
    WHERE TicketId IS NULL
)
UPDATE w
SET TicketId = t.Id
FROM Worklogs w
INNER JOIN RankedWorklogs rw ON rw.Id = w.Id
INNER JOIN RankedTickets t   ON t.ProjectId = rw.ProjectId
                            AND t.rn = (rw.rn % t.cnt);

DELETE FROM Worklogs WHERE TicketId IS NULL;
");

            // 2) Backfill empty descriptions so the new NOT NULL + min-length(16)
            //    invariant holds. Anything shorter is padded with a marker.
            migrationBuilder.Sql(@"
UPDATE Worklogs
SET Description = CONCAT('Backfilled worklog #', LEFT(CONVERT(varchar(36), Id), 8), '.')
WHERE Description IS NULL OR LEN(Description) < 16;
");

            migrationBuilder.DropForeignKey(
                name: "FK_Worklogs_Projects_ProjectId",
                table: "Worklogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Worklogs_Tickets_TicketId",
                table: "Worklogs");

            migrationBuilder.DropIndex(
                name: "IX_Worklogs_ProjectId_Date",
                table: "Worklogs");

            migrationBuilder.DropIndex(
                name: "IX_Worklogs_TicketId",
                table: "Worklogs");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Worklogs");

            migrationBuilder.AlterColumn<Guid>(
                name: "TicketId",
                table: "Worklogs",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Worklogs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Worklogs_TicketId_Date",
                table: "Worklogs",
                columns: new[] { "TicketId", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_Worklogs_Tickets_TicketId",
                table: "Worklogs",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Worklogs_Tickets_TicketId",
                table: "Worklogs");

            migrationBuilder.DropIndex(
                name: "IX_Worklogs_TicketId_Date",
                table: "Worklogs");

            migrationBuilder.AlterColumn<Guid>(
                name: "TicketId",
                table: "Worklogs",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Worklogs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Worklogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Best-effort backfill of ProjectId from the linked ticket so the column is sane.
            migrationBuilder.Sql(@"
UPDATE w
SET w.ProjectId = t.ProjectId
FROM Worklogs w
INNER JOIN Tickets t ON t.Id = w.TicketId;
");

            migrationBuilder.CreateIndex(
                name: "IX_Worklogs_ProjectId_Date",
                table: "Worklogs",
                columns: new[] { "ProjectId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Worklogs_TicketId",
                table: "Worklogs",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Worklogs_Projects_ProjectId",
                table: "Worklogs",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Worklogs_Tickets_TicketId",
                table: "Worklogs",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

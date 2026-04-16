using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftimProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubOAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GitHubAccessToken",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubLogin",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GitHubConnectedByUserId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubAccessToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GitHubLogin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GitHubConnectedByUserId",
                table: "Projects");
        }
    }
}

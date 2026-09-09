using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortalApi.Migrations
{
    /// <inheritdoc />
    public partial class JobPostLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "JobPosts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "JobPosts",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_Status_ExpiresAt",
                table: "JobPosts",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobPosts_Status_ExpiresAt",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "JobPosts");
        }
    }
}

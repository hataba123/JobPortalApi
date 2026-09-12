using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddReliabilityAndUserFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_JobPostId",
                table: "Jobs");

            migrationBuilder.AddColumn<bool>(
                name: "ApplicationUpdates",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailNotifications",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "JobAlerts",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingEmails",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Users",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProfileVisibility",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockExpiresAt",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                table: "OutboxMessages",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyFollows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyFollows_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyFollows_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt_DeadLetteredAt_LockExpiresAt_NextAttemptAt",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "DeadLetteredAt", "LockExpiresAt", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobPostId_Id",
                table: "Jobs",
                columns: new[] { "JobPostId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFollows_CompanyId",
                table: "CompanyFollows",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFollows_UserId_CompanyId",
                table: "CompanyFollows",
                columns: new[] { "UserId", "CompanyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyFollows");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAt_DeadLetteredAt_LockExpiresAt_NextAttemptAt",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_JobPostId_Id",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ApplicationUpdates",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailNotifications",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "JobAlerts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MarketingEmails",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfileVisibility",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockExpiresAt",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobPostId",
                table: "Jobs",
                column: "JobPostId");
        }
    }
}

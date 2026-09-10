using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditRefundMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActorId",
                table: "CreditLedgers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "CreditLedgers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceIdempotencyKey",
                table: "CreditLedgers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgers_SourceIdempotencyKey",
                table: "CreditLedgers",
                column: "SourceIdempotencyKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CreditLedgers_SourceIdempotencyKey",
                table: "CreditLedgers");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "CreditLedgers");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "CreditLedgers");

            migrationBuilder.DropColumn(
                name: "SourceIdempotencyKey",
                table: "CreditLedgers");
        }
    }
}

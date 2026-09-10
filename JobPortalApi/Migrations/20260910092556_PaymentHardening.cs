using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortalApi.Migrations
{
    /// <inheritdoc />
    public partial class PaymentHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntitlementsSnapshot",
                table: "PaymentOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanNameSnapshot",
                table: "PaymentOrders",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceSnapshot",
                table: "PaymentOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EntryType",
                table: "CreditLedgers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "CreditLedgers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Backfill đơn cũ từ gói hiện tại để IPN không gặp snapshot null sau
            // migration. Các đơn tạo mới luôn ghi snapshot ngay khi tạo order.
            migrationBuilder.Sql(@"
UPDATE po
SET
    po.PlanNameSnapshot = sp.Name,
    po.PriceSnapshot = sp.Price,
    po.EntitlementsSnapshot = (
        SELECT
            pe.CreditType AS CreditType,
            pe.Quantity AS Quantity,
            pe.ExpiresInDays AS ExpiresInDays
        FROM PlanEntitlements pe
        WHERE pe.PlanId = po.PlanId
        FOR JSON PATH
    )
FROM PaymentOrders po
INNER JOIN ServicePlans sp ON sp.Id = po.PlanId
WHERE po.PlanNameSnapshot IS NULL;", suppressTransaction: false);

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgers_IdempotencyKey",
                table: "CreditLedgers",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CreditLedgers_IdempotencyKey",
                table: "CreditLedgers");

            migrationBuilder.DropColumn(
                name: "EntitlementsSnapshot",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "PlanNameSnapshot",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "PriceSnapshot",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "EntryType",
                table: "CreditLedgers");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "CreditLedgers");
        }
    }
}

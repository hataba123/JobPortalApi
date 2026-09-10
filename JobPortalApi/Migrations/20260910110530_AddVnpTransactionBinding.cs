using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVnpTransactionBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VnpTransactionNo",
                table: "PaymentOrders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_VnpTransactionNo",
                table: "PaymentOrders",
                column: "VnpTransactionNo",
                unique: true,
                filter: "[VnpTransactionNo] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentOrders_VnpTransactionNo",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "VnpTransactionNo",
                table: "PaymentOrders");
        }
    }
}

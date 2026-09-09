using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortalApi.Migrations
{
    /// <inheritdoc />
    public partial class CompanyVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "Companies",
                type: "datetime2",
                nullable: true);

            // Giữ các công ty hiện hữu đang hiển thị công khai sau khi nâng cấp.
            migrationBuilder.Sql(
                "UPDATE [Companies] SET [VerificationStatus] = 1, [VerifiedAt] = COALESCE([VerifiedAt], SYSUTCDATETIME()) WHERE [VerificationStatus] = 0;");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_VerificationStatus",
                table: "Companies",
                column: "VerificationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_VerificationStatus",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Companies");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortalApi.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_DeletedAt",
                table: "Users",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_DeletedAt",
                table: "JobPosts",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_DeletedAt",
                table: "Companies",
                column: "DeletedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_DeletedAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_JobPosts_DeletedAt",
                table: "JobPosts");

            migrationBuilder.DropIndex(
                name: "IX_Companies_DeletedAt",
                table: "Companies");
        }
    }
}

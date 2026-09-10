using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortalApi.Migrations
{
    /// <inheritdoc />
    public partial class SeniorDataIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CandidateProfiles_UserId",
                table: "CandidateProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "BlogLikes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Đồng bộ bộ đếm denormalized với dữ liệu đơn ứng tuyển trước khi
            // các thao tác tăng/giảm nguyên tử tiếp tục được sử dụng.
            migrationBuilder.Sql(
                "UPDATE jp SET Applicants = (SELECT COUNT(*) FROM Jobs j WHERE j.JobPostId = jp.Id) FROM JobPosts jp;",
                suppressTransaction: false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_UserId",
                table: "CandidateProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlogLikes_UserId_BlogId",
                table: "BlogLikes",
                columns: new[] { "UserId", "BlogId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_CandidateProfiles_UserId",
                table: "CandidateProfiles");

            migrationBuilder.DropIndex(
                name: "IX_BlogLikes_UserId_BlogId",
                table: "BlogLikes");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "BlogLikes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_UserId",
                table: "CandidateProfiles",
                column: "UserId");
        }
    }
}

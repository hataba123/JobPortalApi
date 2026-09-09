using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortalApi.Migrations
{
    /// <inheritdoc />
    public partial class MatchingV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EducationRequirement",
                table: "JobPosts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinExperienceYears",
                table: "JobPosts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedSalary",
                table: "CandidateProfiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExperienceYears",
                table: "CandidateProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredJobType",
                table: "CandidateProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLocation",
                table: "CandidateProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MatchResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobPostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalScore = table.Column<int>(type: "int", nullable: false),
                    BreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatchedSkillsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MissingSkillsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReasonsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InputFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchResults_JobPosts_JobPostId",
                        column: x => x.JobPostId,
                        principalTable: "JobPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchResults_Users_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_CandidateId_JobPostId",
                table: "MatchResults",
                columns: new[] { "CandidateId", "JobPostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_CandidateId_TotalScore",
                table: "MatchResults",
                columns: new[] { "CandidateId", "TotalScore" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_JobPostId_TotalScore",
                table: "MatchResults",
                columns: new[] { "JobPostId", "TotalScore" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchResults");

            migrationBuilder.DropColumn(
                name: "EducationRequirement",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "MinExperienceYears",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "ExpectedSalary",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ExperienceYears",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredJobType",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredLocation",
                table: "CandidateProfiles");
        }
    }
}

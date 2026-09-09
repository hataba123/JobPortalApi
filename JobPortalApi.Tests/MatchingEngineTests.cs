using JobPortalApi.Services.Matching;
using Xunit;

namespace JobPortalApi.Tests;

public class MatchingEngineTests
{
    [Fact]
    public void NormalizesAliasesAndAwardsFullSkillScore()
    {
        var result = MatchingEngine.Calculate(
            new CandidateMatchInput(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "JavaScript, ReactJS, SQL Server",
                null,
                null,
                null,
                null,
                null,
                null,
                null),
            new JobMatchInput(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "js, react, mssql",
                null,
                null,
                null,
                null,
                null,
                null,
                null));

        Assert.Equal(60, result.Breakdown.Skills);
        Assert.Equal(60, result.TotalScore);
        Assert.Empty(result.MissingSkills);
        Assert.Equal("fbbaca4ca39c9f02097aaae91e821b719aea5d9203ede8ed55e663c5254c828b", result.InputFingerprint);
    }

    [Fact]
    public void ReturnsTransparentBreakdownForProfileAndJobRequirements()
    {
        var result = MatchingEngine.Calculate(
            new CandidateMatchInput(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "TypeScript",
                null,
                null,
                4,
                "Đại học Công nghệ",
                "Hà Nội",
                "Full-time",
                1500),
            new JobMatchInput(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "typescript, nodejs",
                null,
                null,
                "Hà Nội",
                "Full-time",
                2000,
                3,
                "dai hoc"));

        Assert.Equal(30, result.Breakdown.Skills);
        Assert.Equal(20, result.Breakdown.Experience);
        Assert.Equal(10, result.Breakdown.Education);
        Assert.Equal(10, result.Breakdown.Preferences);
        Assert.Equal(70, result.TotalScore);
        Assert.Equal(new[] { "nodejs" }, result.MissingSkills);
    }
}

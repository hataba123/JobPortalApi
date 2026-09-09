using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JobPortalApi.Services.Matching
{
    public sealed record CandidateMatchInput(
        Guid Id,
        string? Skills,
        string? Certificates,
        string? Experience,
        int? ExperienceYears,
        string? Education,
        string? PreferredLocation,
        string? PreferredJobType,
        decimal? ExpectedSalary);

    public sealed record JobMatchInput(
        Guid Id,
        string? SkillsRequired,
        string? Tags,
        string? Description,
        string? Location,
        string? Type,
        decimal? Salary,
        int? MinExperienceYears,
        string? EducationRequirement);

    public sealed record MatchBreakdown(int Skills, int Experience, int Education, int Preferences);

    public sealed record CalculatedMatch(
        Guid CandidateId,
        Guid JobPostId,
        int TotalScore,
        string AlgorithmVersion,
        MatchBreakdown Breakdown,
        IReadOnlyList<string> MatchedSkills,
        IReadOnlyList<string> MissingSkills,
        string Reason,
        string InputFingerprint);

    public static class MatchingEngine
    {
        public const string AlgorithmVersion = "v1";

        private static readonly IReadOnlyDictionary<string, string> Aliases =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["js"] = "javascript",
                ["node"] = "nodejs",
                ["node.js"] = "nodejs",
                ["node js"] = "nodejs",
                ["nodejs"] = "nodejs",
                ["ts"] = "typescript",
                ["reactjs"] = "react",
                ["react.js"] = "react",
                ["react js"] = "react",
                ["c#"] = "csharp",
                ["csharp"] = "csharp",
                ["c++"] = "cpp",
                [".net"] = "dotnet",
                ["dotnet"] = "dotnet",
                ["asp.net"] = "aspnet",
                ["asp net"] = "aspnet",
                ["aspnet"] = "aspnet",
                ["postgres"] = "postgresql",
                ["sql server"] = "sqlserver",
                ["mssql"] = "sqlserver",
            };

        public static string NormalizeSkill(string value)
        {
            var normalized = RemoveDiacritics(value)
                .ToLowerInvariant();
            normalized = Regex.Replace(normalized, @"c\s*#", "csharp");
            normalized = Regex.Replace(normalized, @"c\s*\+\+", "cpp");
            normalized = Regex.Replace(normalized, @"\.net", "dotnet");
            normalized = Regex.Replace(normalized, @"[^a-z0-9+#.\s-]", " ");
            normalized = Regex.Replace(normalized, @"[._-]+", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            return Aliases.TryGetValue(normalized, out var alias) ? alias : normalized;
        }

        public static IReadOnlyList<string> SplitSkills(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Array.Empty<string>();
            IEnumerable<string> values;
            var trimmed = value.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                try
                {
                    values = JsonSerializer.Deserialize<List<string>>(trimmed) ?? new List<string>();
                }
                catch (JsonException)
                {
                    values = new[] { value };
                }
            }
            else
            {
                values = Regex.Split(value, "[,;|\\n/]+");
            }

            return values
                .Select(NormalizeSkill)
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public static CalculatedMatch Calculate(CandidateMatchInput candidate, JobMatchInput job)
        {
            var requiredSkills = SplitSkills(job.SkillsRequired).Count > 0
                ? SplitSkills(job.SkillsRequired)
                : SplitSkills(job.Tags);
            var candidateSkills = SplitSkills(candidate.Skills)
                .Concat(SplitSkills(candidate.Certificates))
                .ToHashSet(StringComparer.Ordinal);
            var matchedSkills = requiredSkills.Where(candidateSkills.Contains).ToArray();
            var missingSkills = requiredSkills.Where(skill => !candidateSkills.Contains(skill)).ToArray();
            var skills = requiredSkills.Count == 0
                ? 0
                : RoundScore((double)matchedSkills.Length / requiredSkills.Count * 60);

            var requiredYears = job.MinExperienceYears ?? ParseYears(job.Description);
            var candidateYears = candidate.ExperienceYears ?? ParseYears(candidate.Experience);
            var experience = requiredYears == null || candidateYears == null
                ? 0
                : requiredYears <= 0
                    ? 20
                    : RoundScore(Math.Min((double)candidateYears / requiredYears.Value, 1) * 20);

            var education = !string.IsNullOrWhiteSpace(job.EducationRequirement) &&
                !string.IsNullOrWhiteSpace(candidate.Education) &&
                ContainsNormalized(candidate.Education, job.EducationRequirement) ? 10 : 0;
            var preferences = CalculatePreferences(candidate, job);
            var breakdown = new MatchBreakdown(skills, experience, education, preferences);
            var reason = string.Join("; ", new[]
            {
                requiredSkills.Count > 0
                    ? $"Khớp {matchedSkills.Length}/{requiredSkills.Count} kỹ năng"
                    : "Tin chưa khai báo kỹ năng bắt buộc",
                requiredYears != null
                    ? candidateYears != null
                        ? $"{candidateYears.Value.ToString(CultureInfo.InvariantCulture)} năm kinh nghiệm trên yêu cầu {requiredYears.Value.ToString(CultureInfo.InvariantCulture)} năm"
                        : "Chưa có dữ liệu số năm kinh nghiệm"
                    : "Tin chưa khai báo yêu cầu kinh nghiệm",
                !string.IsNullOrWhiteSpace(job.EducationRequirement)
                    ? education > 0 ? "Đạt yêu cầu học vấn" : "Chưa khớp yêu cầu học vấn"
                    : "Tin chưa khai báo yêu cầu học vấn",
                preferences > 0 ? "Có điểm phù hợp về ưu tiên cá nhân" : "Chưa có dữ liệu ưu tiên cá nhân phù hợp",
            });

            var fingerprintPayload = new
            {
                algorithmVersion = AlgorithmVersion,
                candidate = new
                {
                    id = candidate.Id,
                    skills = candidate.Skills,
                    certificates = candidate.Certificates,
                    experience = candidate.Experience,
                    experienceYears = candidate.ExperienceYears,
                    education = candidate.Education,
                    preferredLocation = candidate.PreferredLocation,
                    preferredJobType = candidate.PreferredJobType,
                    expectedSalary = candidate.ExpectedSalary,
                },
                job = new
                {
                    id = job.Id,
                    skillsRequired = job.SkillsRequired,
                    tags = job.Tags,
                    description = job.Description,
                    location = job.Location,
                    type = job.Type,
                    salary = job.Salary,
                    minExperienceYears = job.MinExperienceYears,
                    educationRequirement = job.EducationRequirement,
                },
            };
            var fingerprintJson = JsonSerializer.Serialize(fingerprintPayload, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintJson))).ToLowerInvariant();

            return new CalculatedMatch(
                candidate.Id,
                job.Id,
                skills + experience + education + preferences,
                AlgorithmVersion,
                breakdown,
                matchedSkills,
                missingSkills,
                reason,
                fingerprint);
        }

        private static int CalculatePreferences(CandidateMatchInput candidate, JobMatchInput job)
        {
            var score = 0;
            if (!string.IsNullOrWhiteSpace(candidate.PreferredLocation) && !string.IsNullOrWhiteSpace(job.Location) &&
                (ContainsNormalized(candidate.PreferredLocation, job.Location) || ContainsNormalized(job.Location, candidate.PreferredLocation)))
                score += 4;
            if (!string.IsNullOrWhiteSpace(candidate.PreferredJobType) && !string.IsNullOrWhiteSpace(job.Type) &&
                ContainsNormalized(candidate.PreferredJobType, job.Type))
                score += 3;
            if (candidate.ExpectedSalary.HasValue && job.Salary.HasValue && job.Salary.Value >= candidate.ExpectedSalary.Value)
                score += 3;
            return score;
        }

        private static bool ContainsNormalized(string? haystack, string? needle)
            => !string.IsNullOrWhiteSpace(haystack) && !string.IsNullOrWhiteSpace(needle) &&
               NormalizeSkill(haystack).Contains(NormalizeSkill(needle), StringComparison.Ordinal);

        private static int? ParseYears(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var match = Regex.Match(RemoveDiacritics(value).ToLowerInvariant(), @"(\d+(?:[.,]\d+)?)\s*\+?\s*(?:years?|yrs?|nam)");
            if (!match.Success || !double.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var years))
                return null;
            return (int)Math.Floor(years);
        }

        private static int RoundScore(double value) => (int)Math.Floor(value + 0.5);

        private static string RemoveDiacritics(string value)
        {
            value = value.Replace('Đ', 'D').Replace('đ', 'd');
            var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }
            return builder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}

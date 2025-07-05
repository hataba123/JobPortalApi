namespace JobPortalApi.DTOs.CandidateProfile
{
    public class CandidateProfileUpdateDto
    {
        // Thêm nếu muốn cho phép cập nhật
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? ResumeUrl { get; set; }
        public string? Experience { get; set; }
        public string? Skills { get; set; }
        public string? Education { get; set; }
        public DateTime? Dob { get; set; }
        public string? Gender { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? GithubUrl { get; set; }
        public string? Certificates { get; set; }
        public string? Summary { get; set; }
    }
}

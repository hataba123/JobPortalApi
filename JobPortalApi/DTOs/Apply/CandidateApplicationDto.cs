using JobPortalApi.Models.Enums;

namespace JobPortalApi.DTOs.Apply
{
    public class CandidateApplicationDto
    {
        public Guid Id { get; set; } // ✅ Thêm dòng này

        public Guid CandidateId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime AppliedAt { get; set; }
        public string? CVUrl { get; set; }
        public ApplyStatus Status { get; set; } // ✅ THÊM DÒNG NÀY

    }

}

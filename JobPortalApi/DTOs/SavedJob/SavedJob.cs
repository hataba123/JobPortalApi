namespace JobPortalApi.DTOs.SavedJob
{
    public class SavedJobDto
    {
        public Guid Id { get; set; }
        public Guid JobPostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Location { get; set; }
        public DateTime SavedAt { get; set; }
    }
}

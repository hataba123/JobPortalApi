namespace JobPortalApi.DTOs.Apply
{
    public class JobApplicationRequest
    {
        public Guid JobPostId { get; set; }
        public string CVUrl { get; set; }
    }
}

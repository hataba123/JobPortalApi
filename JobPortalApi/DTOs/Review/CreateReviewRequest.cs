namespace JobPortalApi.DTOs.Review
{
    public class CreateReviewRequest
    {
        public Guid CompanyId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}

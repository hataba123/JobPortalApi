namespace JobPortalApi.DTOs.AdminReview
{
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid CompanyId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

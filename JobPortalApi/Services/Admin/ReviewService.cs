using JobPortalApi.DTOs.AdminReview;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.Admin
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewDto>> GetAllReviewsAsync()
        {
            return await _context.Review
                .AsNoTracking()
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    CompanyId = r.CompanyId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ReviewDto?> GetReviewByIdAsync(Guid id)
        {
            var r = await _context.Review.FindAsync(id);
            if (r == null) return null;
            return new ReviewDto
            {
                Id = r.Id,
                UserId = r.UserId,
                CompanyId = r.CompanyId,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            };
        }

        public async Task<bool> UpdateReviewAsync(Guid id, UpdateReviewDto dto)
        {
            var r = await _context.Review.FindAsync(id);
            if (r == null) return false;
            if (dto.Rating.HasValue) r.Rating = dto.Rating.Value;
            if (!string.IsNullOrWhiteSpace(dto.Comment)) r.Comment = dto.Comment;
            _context.Review.Update(r);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteReviewAsync(Guid id)
        {
            var r = await _context.Review.FindAsync(id);
            if (r == null) return false;
            _context.Review.Remove(r);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

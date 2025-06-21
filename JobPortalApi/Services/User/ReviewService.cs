using JobPortalApi.DTOs.Review;
using JobPortalApi.Models;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.User
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ReviewDto>> GetByCompanyAsync(Guid companyId)
        {
            return await _context.Review
                .Where(r => r.CompanyId == companyId)
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

        public async Task CreateAsync(Guid userId, CreateReviewRequest request)
        {
            var review = new Review
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CompanyId = request.CompanyId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Review.Add(review);
            await _context.SaveChangesAsync();
        }
    }
}


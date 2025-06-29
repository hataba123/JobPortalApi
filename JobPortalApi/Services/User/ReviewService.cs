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
        public async Task<IEnumerable<ReviewDto>> GetAllAsync()
        {
            return await _context.Review
                .Select(c => new ReviewDto
                {
                    Id = c.Id,
                    Comment = c.Comment,
                    CompanyId = c.CompanyId,
                    CreatedAt = DateTime.Now,
                    Rating = c.Rating,
                    UserId = c.UserId
                })
                .ToListAsync();
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

            await UpdateCompanyRatingAsync(request.CompanyId); // ✅ Thêm dòng này

        }
        private async Task UpdateCompanyRatingAsync(Guid companyId)
        {
            var averageRating = await _context.Review
                .Where(r => r.CompanyId == companyId)
                .AverageAsync(r => r.Rating);

            var company = await _context.Companies.FindAsync(companyId);
            if (company != null)
            {
                company.Rating = averageRating;
                await _context.SaveChangesAsync();
            }
        }
    }
}


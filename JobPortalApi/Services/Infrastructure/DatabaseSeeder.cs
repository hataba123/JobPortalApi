using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.Infrastructure
{
    public static class DatabaseSeeder
    {
        private static readonly Guid AdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private static readonly Guid RecruiterId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        private static readonly Guid CandidateId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        private static readonly Guid CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private static readonly Guid CompanyId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        private static readonly Guid JobPostId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        private static readonly Guid PlanId = Guid.Parse("40000000-0000-0000-0000-000000000001");

        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            var password = configuration["Seed:Password"]
                ?? Environment.GetEnvironmentVariable("SEED_PASSWORD");
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("SEED_PASSWORD là bắt buộc khi chạy seed.");

            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var category = await context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == CategoryId);
            if (category == null)
            {
                category = new Category { Id = CategoryId, Name = "Công nghệ thông tin", Icon = "code", Color = "#2563eb" };
                context.Categories.Add(category);
            }

            await UpsertUserAsync(context, AdminId, "seed-admin@example.test", "Seed Admin", UserRole.Admin, passwordHash);
            await UpsertUserAsync(context, RecruiterId, "seed-recruiter@example.test", "Seed Recruiter", UserRole.Recruiter, passwordHash);
            await UpsertUserAsync(context, CandidateId, "seed-candidate@example.test", "Seed Candidate", UserRole.Candidate, passwordHash);

            var company = await context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == CompanyId);
            if (company == null)
            {
                company = new Company { Id = CompanyId };
                context.Companies.Add(company);
            }
            company.Name = "JobPortal Demo Company";
            company.Description = "Công ty giả lập dùng cho môi trường phát triển.";
            company.Location = "Hà Nội";
            company.Employees = "51-200";
            company.Industry = "Công nghệ thông tin";
            company.OpenJobs = 1;
            company.Rating = 5;
            company.UserId = RecruiterId;
            company.DeletedAt = null;
            company.VerificationStatus = CompanyVerificationStatus.Verified;
            company.VerifiedAt = DateTime.UtcNow;

            var jobPost = await context.JobPosts.IgnoreQueryFilters().FirstOrDefaultAsync(j => j.Id == JobPostId);
            if (jobPost == null)
            {
                jobPost = new JobPost { Id = JobPostId };
                context.JobPosts.Add(jobPost);
            }
            jobPost.Title = "Backend Engineer (Demo)";
            jobPost.Description = "Tin tuyển dụng giả lập để kiểm thử luồng ứng tuyển và matching.";
            jobPost.SkillsRequired = "TypeScript, NestJS, PostgreSQL";
            jobPost.Location = "Hà Nội";
            jobPost.Salary = 25000000;
            jobPost.EmployerId = RecruiterId;
            jobPost.CompanyId = CompanyId;
            jobPost.Type = "Full-time";
            jobPost.Tags = new List<string> { "NestJS", "PostgreSQL" };
            jobPost.Applicants = 0;
            jobPost.CreatedAt = DateTime.UtcNow;
            jobPost.DeletedAt = null;
            jobPost.ExpiresAt = DateTime.UtcNow.AddDays(30);
            jobPost.Status = JobPostStatus.Active;
            jobPost.MinExperienceYears = 2;
            jobPost.EducationRequirement = "Đại học";
            jobPost.CategoryId = CategoryId;

            var plan = await context.ServicePlans.FirstOrDefaultAsync(p => p.Id == PlanId);
            if (plan == null)
            {
                plan = new ServicePlan { Id = PlanId };
                context.ServicePlans.Add(plan);
            }
            plan.Name = "Demo Recruiter Pack";
            plan.Price = 99000;
            plan.Currency = "VND";
            plan.IsActive = true;
            var entitlement = await context.PlanEntitlements.FirstOrDefaultAsync(e => e.PlanId == PlanId && e.CreditType == CreditType.JobPost);
            if (entitlement == null)
            {
                context.PlanEntitlements.Add(new PlanEntitlement
                {
                    Id = Guid.NewGuid(),
                    PlanId = PlanId,
                    CreditType = CreditType.JobPost,
                    Quantity = 3,
                    ExpiresInDays = 30
                });
            }
            else
            {
                entitlement.Quantity = 3;
                entitlement.ExpiresInDays = 30;
            }

            await context.SaveChangesAsync();
        }

        private static async Task UpsertUserAsync(
            ApplicationDbContext context,
            Guid id,
            string email,
            string fullName,
            UserRole role,
            string passwordHash)
        {
            var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                user = new JobPortalApi.Models.User { Id = id };
                context.Users.Add(user);
            }
            user.Email = email;
            user.FullName = fullName;
            user.Role = role;
            user.PasswordHash = passwordHash;
            user.PasswordVersion = 0;
            user.DeletedAt = null;
            user.CreatedAt = user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt;
        }
    }
}

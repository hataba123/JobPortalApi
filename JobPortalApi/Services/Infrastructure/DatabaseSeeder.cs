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
            company.Logo = "/uploads/logo/jobportal-demo.svg";

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
            jobPost.Logo = "/uploads/logo/jobportal-demo.svg";

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

            var blogAuthor = await UpsertBlogAuthorAsync(
                context,
                "JobPortal Editorial Team",
                "/uploads/logo/jobportal-demo.svg",
                "Admin");
            await context.SaveChangesAsync();

            await UpsertBlogAsync(
                context,
                blogAuthor.Id,
                "xay-dung-ho-so-nghe-nghiep-noi-bat",
                "Cách xây dựng hồ sơ nghề nghiệp nổi bật năm 2026",
                "Một hồ sơ rõ ràng, có số liệu và tập trung vào kết quả giúp nhà tuyển dụng hiểu nhanh giá trị của bạn.",
                "Bắt đầu bằng phần giới thiệu ngắn, sau đó ưu tiên thành tựu có thể đo lường và các kỹ năng phù hợp với vị trí đang ứng tuyển. Hãy cập nhật hồ sơ định kỳ để phản ánh đúng kinh nghiệm mới nhất.",
                "Phát triển sự nghiệp",
                new[] { "CV", "Career", "Job search" },
                "6 phút",
                true,
                "/uploads/images/blog-career-profile.jpg",
                new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));
            await UpsertBlogAsync(
                context,
                blogAuthor.Id,
                "ky-nang-cong-tac-trong-doi-ngu",
                "Kỹ năng cộng tác giúp bạn nổi bật trong đội ngũ",
                "Giao tiếp chủ động, phản hồi có cấu trúc và tinh thần chia sẻ là nền tảng của mọi đội ngũ hiệu quả.",
                "Khi làm việc nhóm, hãy thống nhất mục tiêu, ghi nhận trách nhiệm và chia sẻ tiến độ minh bạch. Những thói quen nhỏ này giúp giảm hiểu nhầm và tạo niềm tin lâu dài giữa các thành viên.",
                "Kỹ năng",
                new[] { "Teamwork", "Soft skills", "Productivity" },
                "5 phút",
                true,
                "/uploads/images/blog-team-collaboration.jpg",
                new DateTime(2026, 8, 12, 8, 0, 0, DateTimeKind.Utc));
            await UpsertBlogAsync(
                context,
                blogAuthor.Id,
                "checklist-chuan-bi-phong-van-cong-nghe",
                "Checklist chuẩn bị phỏng vấn vị trí công nghệ",
                "Từ nghiên cứu công ty đến phần trình bày dự án, đây là checklist ngắn giúp bạn tự tin trước buổi phỏng vấn.",
                "Đọc kỹ mô tả công việc, chuẩn bị hai đến ba câu chuyện theo mô hình STAR và kiểm tra lại các dự án có liên quan. Cuối buổi, hãy đặt câu hỏi về đội ngũ, kỳ vọng 90 ngày đầu và cách đo lường thành công.",
                "Phỏng vấn",
                new[] { "Interview", "Technology", "Preparation" },
                "7 phút",
                false,
                "/uploads/images/blog-tech-workspace.jpg",
                new DateTime(2026, 8, 5, 8, 0, 0, DateTimeKind.Utc));

            await context.SaveChangesAsync();
        }

        private static async Task<BlogAuthor> UpsertBlogAuthorAsync(
            ApplicationDbContext context,
            string name,
            string avatar,
            string role)
        {
            var author = await context.BlogAuthors.FirstOrDefaultAsync(item => item.Name == name);
            if (author == null)
            {
                author = new BlogAuthor { Name = name };
                context.BlogAuthors.Add(author);
            }

            author.Name = name;
            author.Avatar = avatar;
            author.Role = role;
            return author;
        }

        private static async Task UpsertBlogAsync(
            ApplicationDbContext context,
            int authorId,
            string slug,
            string title,
            string excerpt,
            string content,
            string category,
            string[] tags,
            string readTime,
            bool featured,
            string image,
            DateTime publishedAt)
        {
            var blog = await context.Blogs.FirstOrDefaultAsync(item => item.Slug == slug);
            if (blog == null)
            {
                blog = new Blog { Slug = slug };
                context.Blogs.Add(blog);
            }

            blog.Title = title;
            blog.Excerpt = excerpt;
            blog.Content = content;
            blog.Slug = slug;
            blog.Category = category;
            blog.SetTagsArray(tags);
            blog.PublishedAt = publishedAt;
            blog.ReadTime = readTime;
            blog.Featured = featured;
            blog.Image = image;
            blog.AuthorId = authorId;
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

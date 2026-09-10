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
        private static readonly Guid RecruiterSecondId = Guid.Parse("00000000-0000-0000-0000-000000000004");
        private static readonly Guid CandidateSecondId = Guid.Parse("00000000-0000-0000-0000-000000000005");
        private static readonly Guid CandidateThirdId = Guid.Parse("00000000-0000-0000-0000-000000000006");
        private static readonly Guid CandidateFourthId = Guid.Parse("00000000-0000-0000-0000-000000000007");
        private static readonly Guid CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private static readonly Guid FrontendCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        private static readonly Guid DataCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        private static readonly Guid DevopsCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000004");
        private static readonly Guid MobileCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000005");
        private static readonly Guid CompanyId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        private static readonly Guid MicrosoftCompanyId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        private static readonly Guid GoogleCompanyId = Guid.Parse("20000000-0000-0000-0000-000000000003");
        private static readonly Guid AmazonCompanyId = Guid.Parse("20000000-0000-0000-0000-000000000004");
        private static readonly Guid GithubCompanyId = Guid.Parse("20000000-0000-0000-0000-000000000005");
        private static readonly Guid AppleCompanyId = Guid.Parse("20000000-0000-0000-0000-000000000006");
        private static readonly Guid JobPostId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        private static readonly Guid FrontendJobPostId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        private static readonly Guid DataJobPostId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        private static readonly Guid DevopsJobPostId = Guid.Parse("30000000-0000-0000-0000-000000000004");
        private static readonly Guid MobileJobPostId = Guid.Parse("30000000-0000-0000-0000-000000000005");
        private static readonly Guid ProductJobPostId = Guid.Parse("30000000-0000-0000-0000-000000000006");
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

            await UpsertCategoryAsync(context, FrontendCategoryId, "Frontend & UI", "layout", "#7c3aed");
            await UpsertCategoryAsync(context, DataCategoryId, "Dữ liệu & AI", "database", "#0891b2");
            await UpsertCategoryAsync(context, DevopsCategoryId, "DevOps & Cloud", "cloud", "#ea580c");
            await UpsertCategoryAsync(context, MobileCategoryId, "Mobile", "smartphone", "#16a34a");

            await UpsertUserAsync(context, AdminId, "seed-admin@example.test", "Seed Admin", UserRole.Admin, passwordHash);
            await UpsertUserAsync(context, RecruiterId, "seed-recruiter@example.test", "Seed Recruiter", UserRole.Recruiter, passwordHash);
            await UpsertUserAsync(context, CandidateId, "seed-candidate@example.test", "Seed Candidate", UserRole.Candidate, passwordHash);
            await UpsertUserAsync(context, RecruiterSecondId, "seed-recruiter-2@example.test", "Seed Recruiter 2", UserRole.Recruiter, passwordHash);
            await UpsertCandidateProfileAsync(context, CandidateId, "seed-candidate@example.test", "Seed Candidate", "TypeScript, NestJS, PostgreSQL", 3, "Đại học Công nghệ", "Hà Nội", "Full-time", passwordHash);
            await UpsertCandidateProfileAsync(context, CandidateSecondId, "seed-candidate-2@example.test", "Nguyễn Minh Anh", "React, TypeScript, Next.js", 4, "Đại học Bách khoa", "Hồ Chí Minh", "Full-time", passwordHash);
            await UpsertCandidateProfileAsync(context, CandidateThirdId, "seed-candidate-3@example.test", "Trần Quốc Bảo", "Python, SQL, Machine Learning", 3, "Đại học Công nghệ", "Đà Nẵng", "Full-time", passwordHash);
            await UpsertCandidateProfileAsync(context, CandidateFourthId, "seed-candidate-4@example.test", "Lê Hoàng Nam", "AWS, Docker, Kubernetes", 5, "Đại học FPT", "Hà Nội", "Remote", passwordHash);

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
            company.Website = "https://jobportal.local";
            company.Founded = "2024";
            company.Tags = "JobPortal,Recruitment,Technology";
            company.UserId = RecruiterId;
            company.DeletedAt = null;
            company.VerificationStatus = CompanyVerificationStatus.Verified;
            company.VerifiedAt = DateTime.UtcNow;
            company.Logo = "/uploads/logo/jobportal-demo.svg";

            await UpsertCompanyAsync(
                context,
                MicrosoftCompanyId,
                "Microsoft Vietnam (Demo)",
                "/uploads/logo/company-microsoft.svg",
                "Hà Nội",
                "1000+",
                "Công nghệ thông tin",
                4.8,
                "https://www.microsoft.com",
                "1975",
                "Cloud,AI,Engineering");
            await UpsertCompanyAsync(
                context,
                GoogleCompanyId,
                "Google Vietnam (Demo)",
                "/uploads/logo/company-google.svg",
                "Hồ Chí Minh",
                "1000+",
                "Công nghệ thông tin",
                4.9,
                "https://about.google",
                "1998",
                "Search,Cloud,AI");
            await UpsertCompanyAsync(
                context,
                AmazonCompanyId,
                "Amazon Web Services (Demo)",
                "/uploads/logo/company-amazon.svg",
                "Đà Nẵng",
                "501-1000",
                "Điện toán đám mây",
                4.7,
                "https://aws.amazon.com",
                "2006",
                "AWS,Cloud,DevOps");
            await UpsertCompanyAsync(
                context,
                GithubCompanyId,
                "GitHub Vietnam (Demo)",
                "/uploads/logo/company-github.svg",
                "Hà Nội",
                "201-500",
                "Nền tảng phát triển",
                4.6,
                "https://github.com",
                "2008",
                "Git,Open source,Developer tools");
            await UpsertCompanyAsync(
                context,
                AppleCompanyId,
                "Apple Developer (Demo)",
                "/uploads/logo/company-apple.svg",
                "Hồ Chí Minh",
                "1000+",
                "Sản phẩm công nghệ",
                4.8,
                "https://developer.apple.com",
                "1976",
                "iOS,Swift,Design");

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

            await UpsertJobPostAsync(context, FrontendJobPostId, "Frontend Developer (Demo)", "Xây dựng giao diện tuyển dụng tốc độ cao và thân thiện trên nhiều thiết bị.", "React, TypeScript, Next.js", "Hồ Chí Minh", 28000000, RecruiterId, GoogleCompanyId, "Full-time", new List<string> { "React", "Next.js", "TypeScript" }, FrontendCategoryId, 2, "Đại học", "/uploads/logo/company-google.svg");
            await UpsertJobPostAsync(context, DataJobPostId, "Data Engineer (Demo)", "Thiết kế pipeline dữ liệu và mô hình báo cáo cho sản phẩm công nghệ.", "Python, SQL, Airflow", "Đà Nẵng", 32000000, RecruiterId, AmazonCompanyId, "Full-time", new List<string> { "Python", "SQL", "Data" }, DataCategoryId, 3, "Đại học", "/uploads/logo/company-amazon.svg");
            await UpsertJobPostAsync(context, DevopsJobPostId, "Cloud DevOps Engineer (Demo)", "Vận hành hạ tầng cloud an toàn, tự động hóa triển khai và giám sát hệ thống.", "AWS, Docker, Kubernetes", "Remote", 38000000, RecruiterId, MicrosoftCompanyId, "Remote", new List<string> { "AWS", "Docker", "Kubernetes" }, DevopsCategoryId, 4, "Đại học", "/uploads/logo/company-microsoft.svg");
            await UpsertJobPostAsync(context, MobileJobPostId, "Mobile Developer (Demo)", "Phát triển trải nghiệm mobile mượt mà cho ứng dụng tìm việc JobPortal.", "Swift, iOS, REST API", "Hồ Chí Minh", 30000000, RecruiterId, AppleCompanyId, "Full-time", new List<string> { "Swift", "iOS", "Mobile" }, MobileCategoryId, 2, "Cao đẳng", "/uploads/logo/company-apple.svg");
            await UpsertJobPostAsync(context, ProductJobPostId, "Product Designer (Demo)", "Thiết kế trải nghiệm người dùng và hệ thống giao diện cho nền tảng tuyển dụng.", "Figma, UX Research, Design System", "Hà Nội", 26000000, RecruiterId, GithubCompanyId, "Hybrid", new List<string> { "Figma", "UX", "Product" }, FrontendCategoryId, 2, "Không bắt buộc", "/uploads/logo/company-github.svg");

            await UpsertJobAsync(context, Guid.Parse("50000000-0000-0000-0000-000000000001"), JobPostId, CandidateId, ApplyStatus.Screening);
            await UpsertJobAsync(context, Guid.Parse("50000000-0000-0000-0000-000000000002"), FrontendJobPostId, CandidateSecondId, ApplyStatus.Offer);
            await UpsertJobAsync(context, Guid.Parse("50000000-0000-0000-0000-000000000003"), DataJobPostId, CandidateThirdId, ApplyStatus.Applied);
            await UpsertJobAsync(context, Guid.Parse("50000000-0000-0000-0000-000000000004"), DevopsJobPostId, CandidateFourthId, ApplyStatus.Screening);
            await UpsertSavedJobAsync(context, Guid.Parse("60000000-0000-0000-0000-000000000001"), CandidateId, DevopsJobPostId);
            await UpsertSavedJobAsync(context, Guid.Parse("60000000-0000-0000-0000-000000000002"), CandidateSecondId, MobileJobPostId);

            jobPost.Applicants = 1;
            await SetJobApplicantsAsync(context, FrontendJobPostId, 1);
            await SetJobApplicantsAsync(context, DataJobPostId, 1);
            await SetJobApplicantsAsync(context, DevopsJobPostId, 1);
            await SetCompanyOpenJobsAsync(context, CompanyId, 1);
            await SetCompanyOpenJobsAsync(context, GoogleCompanyId, 1);
            await SetCompanyOpenJobsAsync(context, AmazonCompanyId, 1);
            await SetCompanyOpenJobsAsync(context, MicrosoftCompanyId, 1);
            await SetCompanyOpenJobsAsync(context, AppleCompanyId, 1);
            await SetCompanyOpenJobsAsync(context, GithubCompanyId, 1);

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
            await UpsertBlogAsync(
                context,
                blogAuthor.Id,
                "lo-trinh-hoc-lap-trinh-vien-moi-bat-dau",
                "Lộ trình học dành cho lập trình viên mới bắt đầu",
                "Một lộ trình thực tế giúp bạn đi từ nền tảng lập trình đến dự án đầu tiên và hồ sơ ứng tuyển.",
                "Hãy chọn một ngôn ngữ chính, nắm chắc cấu trúc dữ liệu cơ bản rồi xây dựng các dự án nhỏ có thể trình bày. Sau mỗi dự án, ghi lại bài học, cải thiện mã nguồn và cập nhật hồ sơ để tiến bộ đều đặn.",
                "Học tập",
                new[] { "Learning", "Programming", "Roadmap" },
                "8 phút",
                false,
                "/uploads/images/blog-tech-workspace.jpg",
                new DateTime(2026, 7, 28, 8, 0, 0, DateTimeKind.Utc));
            await UpsertBlogAsync(
                context,
                blogAuthor.Id,
                "lam-viec-remote-an-toan-va-hieu-qua",
                "Làm việc remote an toàn và hiệu quả",
                "Các thói quen bảo mật và quản lý thời gian cần thiết khi làm việc từ xa trong đội ngũ công nghệ.",
                "Sử dụng trình quản lý mật khẩu, bật xác thực đa yếu tố và chỉ truy cập tài nguyên công ty qua thiết bị được bảo vệ. Bên cạnh đó, hãy chia nhỏ mục tiêu trong ngày và thống nhất giờ cộng tác với đồng đội.",
                "Làm việc",
                new[] { "Remote", "Security", "Productivity" },
                "6 phút",
                false,
                "/uploads/images/blog-team-collaboration.jpg",
                new DateTime(2026, 7, 21, 8, 0, 0, DateTimeKind.Utc));
            await UpsertBlogAsync(
                context,
                blogAuthor.Id,
                "ky-nang-phong-van-system-design",
                "Chuẩn bị phỏng vấn System Design như thế nào?",
                "Khung tư duy đơn giản để phân tích yêu cầu và trình bày thiết kế hệ thống rõ ràng trong buổi phỏng vấn.",
                "Bắt đầu bằng việc làm rõ lưu lượng, dữ liệu và yêu cầu phi chức năng. Sau đó trình bày kiến trúc tổng quan, các điểm nghẽn có thể xảy ra và cách mở rộng theo từng giai đoạn.",
                "Phỏng vấn",
                new[] { "Interview", "Architecture", "System Design" },
                "9 phút",
                true,
                "/uploads/images/blog-career-profile.jpg",
                new DateTime(2026, 7, 14, 8, 0, 0, DateTimeKind.Utc));
            await UpsertBlogAsync(
                context,
                blogAuthor.Id,
                "xay-dung-thuong-hieu-ca-nhan-tren-github",
                "Xây dựng thương hiệu cá nhân trên GitHub",
                "Biến GitHub thành hồ sơ năng lực giúp nhà tuyển dụng hiểu rõ hơn về cách bạn xây dựng sản phẩm.",
                "Chọn một vài dự án tiêu biểu, viết README dễ đọc và duy trì lịch sử commit rõ ràng. Những issue, pull request và tài liệu kỹ thuật chất lượng cũng thể hiện khả năng làm việc nhóm của bạn.",
                "Nghề nghiệp",
                new[] { "GitHub", "Portfolio", "Career" },
                "7 phút",
                false,
                "/uploads/images/blog-tech-workspace.jpg",
                new DateTime(2026, 7, 7, 8, 0, 0, DateTimeKind.Utc));
            await UpsertBlogAsync(
                context,
                blogAuthor.Id,
                "xu-huong-cong-nghe-cho-su-nghiep-it",
                "Xu hướng công nghệ đáng chú ý cho sự nghiệp IT",
                "Những nhóm công nghệ đang tạo ra nhiều cơ hội việc làm và cách chọn hướng phát triển phù hợp.",
                "Cloud, dữ liệu, an toàn thông tin và tự động hóa tiếp tục mở rộng ở nhiều ngành. Thay vì chạy theo mọi xu hướng, hãy chọn một hướng phù hợp với nền tảng hiện tại và đầu tư đủ sâu qua dự án thực tế.",
                "Công nghệ",
                new[] { "Technology", "Cloud", "Data" },
                "6 phút",
                false,
                "/uploads/images/blog-team-collaboration.jpg",
                new DateTime(2026, 6, 30, 8, 0, 0, DateTimeKind.Utc));

            await context.SaveChangesAsync();
        }

        private static async Task UpsertCategoryAsync(ApplicationDbContext context, Guid id, string name, string icon, string color)
        {
            var category = await context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == id);
            if (category == null)
            {
                category = new Category { Id = id };
                context.Categories.Add(category);
            }

            category.Name = name;
            category.Icon = icon;
            category.Color = color;
        }

        private static async Task UpsertCandidateProfileAsync(
            ApplicationDbContext context,
            Guid userId,
            string email,
            string fullName,
            string skills,
            int experienceYears,
            string education,
            string preferredLocation,
            string preferredJobType,
            string passwordHash)
        {
            await UpsertUserAsync(context, userId, email, fullName, UserRole.Candidate, passwordHash);
            var profile = await context.candidateProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.UserId == userId);
            if (profile == null)
            {
                profile = new CandidateProfile { Id = Guid.NewGuid(), UserId = userId };
                context.candidateProfiles.Add(profile);
            }

            profile.Skills = skills;
            profile.ExperienceYears = experienceYears;
            profile.Education = education;
            profile.PreferredLocation = preferredLocation;
            profile.PreferredJobType = preferredJobType;
        }

        private static async Task UpsertJobPostAsync(
            ApplicationDbContext context,
            Guid id,
            string title,
            string description,
            string skillsRequired,
            string location,
            decimal salary,
            Guid employerId,
            Guid companyId,
            string type,
            List<string> tags,
            Guid categoryId,
            int minExperienceYears,
            string educationRequirement,
            string logo)
        {
            var jobPost = await context.JobPosts.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == id);
            if (jobPost == null)
            {
                jobPost = new JobPost { Id = id };
                context.JobPosts.Add(jobPost);
            }

            jobPost.Title = title;
            jobPost.Description = description;
            jobPost.SkillsRequired = skillsRequired;
            jobPost.Location = location;
            jobPost.Salary = salary;
            jobPost.EmployerId = employerId;
            jobPost.CompanyId = companyId;
            jobPost.Type = type;
            jobPost.Tags = tags;
            jobPost.Applicants = 0;
            jobPost.CreatedAt = jobPost.CreatedAt == default ? DateTime.UtcNow : jobPost.CreatedAt;
            jobPost.DeletedAt = null;
            jobPost.ExpiresAt = DateTime.UtcNow.AddDays(30);
            jobPost.Status = JobPostStatus.Active;
            jobPost.MinExperienceYears = minExperienceYears;
            jobPost.EducationRequirement = educationRequirement;
            jobPost.CategoryId = categoryId;
            jobPost.Logo = logo;
        }

        private static async Task UpsertJobAsync(ApplicationDbContext context, Guid id, Guid jobPostId, Guid candidateId, ApplyStatus status)
        {
            var job = await context.Jobs.FirstOrDefaultAsync(item => item.Id == id);
            if (job == null)
            {
                job = new Job { Id = id };
                context.Jobs.Add(job);
            }

            job.JobPostId = jobPostId;
            job.CandidateId = candidateId;
            job.AppliedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
            job.CVUrl = "/uploads/cv/demo-cv.pdf";
            job.Status = status;
        }

        private static async Task UpsertSavedJobAsync(ApplicationDbContext context, Guid id, Guid userId, Guid jobPostId)
        {
            var savedJob = await context.SavedJobs.FirstOrDefaultAsync(item => item.UserId == userId && item.JobPostId == jobPostId);
            if (savedJob == null)
            {
                context.SavedJobs.Add(new SavedJob
                {
                    Id = id,
                    UserId = userId,
                    JobPostId = jobPostId,
                    SavedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc)
                });
            }
        }

        private static async Task SetJobApplicantsAsync(ApplicationDbContext context, Guid jobPostId, int applicants)
        {
            var jobPost = await context.JobPosts.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == jobPostId);
            if (jobPost != null) jobPost.Applicants = applicants;
        }

        private static async Task SetCompanyOpenJobsAsync(ApplicationDbContext context, Guid companyId, int openJobs)
        {
            var company = await context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == companyId);
            if (company != null) company.OpenJobs = openJobs;
        }

        private static async Task UpsertCompanyAsync(
            ApplicationDbContext context,
            Guid id,
            string name,
            string logo,
            string location,
            string employees,
            string industry,
            double rating,
            string website,
            string founded,
            string tags)
        {
            var company = await context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == id);
            if (company == null)
            {
                company = new Company { Id = id };
                context.Companies.Add(company);
            }

            company.Name = name;
            company.Logo = logo;
            company.Description = "Dữ liệu công ty demo dùng để kiểm tra hiển thị logo thương hiệu.";
            company.Location = location;
            company.Employees = employees;
            company.Industry = industry;
            company.OpenJobs = 0;
            company.Rating = rating;
            company.Website = website;
            company.Founded = founded;
            company.Tags = tags;
            company.UserId = RecruiterId;
            company.DeletedAt = null;
            company.VerificationStatus = CompanyVerificationStatus.Verified;
            company.VerifiedAt = DateTime.UtcNow;
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

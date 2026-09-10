using Microsoft.EntityFrameworkCore;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using System.Text.Json;
using System.Security.Cryptography;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    // Entity Framework Core tạo các bảng tương ứng dựa trên các DbSet<T> mà bạn khai báo trong ApplicationDbContext.
    public DbSet<JobPost> JobPosts { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<BlogAuthor> BlogAuthors { get; set; }
    public DbSet<BlogCategory> BlogCategories { get; set; }
    public DbSet<BlogView> BlogViews { get; set; }
    public DbSet<BlogLike> BlogLikes { get; set; }

    public DbSet<Company> Companies { get; set; }
    public DbSet<SavedJob> SavedJobs { get; set; }
    public DbSet<Category> Categories { get; set; } // Thêm DbSet<Category> nếu có
    public DbSet<CandidateProfile> candidateProfiles { get; set; } // Thêm DbSet<CategoryProfile> nếu có
    public DbSet<Notification> Notifications { get; set; } // Thêm DbSet<Notification> nếu có
    public DbSet<OAuthAccount> OAuthAccounts { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<MatchResult> MatchResults { get; set; }
    public DbSet<ServicePlan> ServicePlans { get; set; }
    public DbSet<PlanEntitlement> PlanEntitlements { get; set; }
    public DbSet<PaymentOrder> PaymentOrders { get; set; }
    public DbSet<CreditLedger> CreditLedgers { get; set; }

    public DbSet<Review> Review { get; set; } // Thêm DbSet<Review> nếu có
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories { get; set; }
    public DbSet<Interview> Interviews { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<NewsletterSubscription> NewsletterSubscriptions { get; set; }
    public DbSet<CandidateSkill> CandidateSkills { get; set; }
    public DbSet<JobReport> JobReports { get; set; }

    // Thêm các DbSet khác nếu có

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Map bảng JobPost thành "JobPosts"
        modelBuilder.Entity<JobPost>(entity =>
        {
            entity.ToTable("JobPosts");

            entity.HasIndex(j => new { j.Status, j.ExpiresAt });
            entity.Property(j => j.Status).HasDefaultValue(JobPostStatus.Active);

            entity.Property(e => e.Tags)
                .HasConversion(
                        v => JsonSerializer.Serialize(v ?? new List<string>(), (JsonSerializerOptions?)null),
                        v => string.IsNullOrWhiteSpace(v)
                            ? new List<string>()
                            : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null))
                .HasColumnType("nvarchar(max)");
        });

        // Nếu muốn map bảng Users cũng vậy
        modelBuilder.Entity<User>()
            .ToTable("Users");
        modelBuilder.Entity<User>()
            .HasQueryFilter(user => user.DeletedAt == null)
            .HasIndex(user => user.DeletedAt);

        modelBuilder.Entity<Company>()
            .HasQueryFilter(company => company.DeletedAt == null)
            .HasIndex(company => company.DeletedAt);
        modelBuilder.Entity<Company>()
            .Property(company => company.VerificationStatus)
            .HasDefaultValue(CompanyVerificationStatus.Pending);
        modelBuilder.Entity<Company>()
            .HasIndex(company => company.VerificationStatus);

        modelBuilder.Entity<JobPost>()
            .HasQueryFilter(jobPost => jobPost.DeletedAt == null)
            .HasIndex(jobPost => jobPost.DeletedAt);

        modelBuilder.Entity<OAuthAccount>()
            .HasIndex(a => new { a.Provider, a.ProviderAccountId })
            .IsUnique();

        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        modelBuilder.Entity<Job>()
               .ToTable("Jobs"); // Map Job thành bảng Applies
        modelBuilder.Entity<Job>().Property(a => a.RowVersion).IsRowVersion();
        modelBuilder.Entity<Job>()

             .HasOne(a => a.JobPost)
             .WithMany()
             .HasForeignKey(a => a.JobPostId)
             .OnDelete(DeleteBehavior.Cascade); // OK vì JobPost chỉ có 1 cascade

        modelBuilder.Entity<Job>()
            .HasOne(a => a.Candidate)
            .WithMany()
            .HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Restrict); // FIX lỗi cascade bằng cách không cascade ở đây
        modelBuilder.Entity<Job>()
            .HasIndex(a => new { a.CandidateId, a.JobPostId })
            .IsUnique();

        modelBuilder.Entity<ApplicationStatusHistory>()
            .HasOne(history => history.Application)
            .WithMany()
            .HasForeignKey(history => history.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ApplicationStatusHistory>()
            .HasIndex(history => new { history.ApplicationId, history.ChangedAt });

        modelBuilder.Entity<Interview>()
            .Property(interview => interview.RowVersion)
            .IsRowVersion();
        modelBuilder.Entity<Interview>()
            .HasOne(interview => interview.Application)
            .WithMany()
            .HasForeignKey(interview => interview.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Interview>()
            .HasOne(interview => interview.Interviewer)
            .WithMany()
            .HasForeignKey(interview => interview.InterviewerId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Interview>()
            .HasIndex(interview => new { interview.InterviewerId, interview.StartAt, interview.Status });
        modelBuilder.Entity<Interview>()
            .HasIndex(interview => new { interview.ApplicationId, interview.StartAt });

        modelBuilder.Entity<JobPost>().Property(post => post.RowVersion).IsRowVersion();
        modelBuilder.Entity<Company>().Property(company => company.RowVersion).IsRowVersion();

        modelBuilder.Entity<AuditLog>()
            .HasIndex(log => new { log.EntityType, log.EntityId, log.CreatedAt });
        modelBuilder.Entity<AuditLog>()
            .HasIndex(log => new { log.ActorId, log.CreatedAt });

        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(message => new { message.ProcessedAt, message.NextAttemptAt, message.OccurredAt });
        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(message => message.DeduplicationKey)
            .IsUnique()
            .HasFilter("[DeduplicationKey] IS NOT NULL");

        modelBuilder.Entity<NewsletterSubscription>()
            .HasIndex(subscription => new { subscription.Email, subscription.IsActive })
            .IsUnique();

        modelBuilder.Entity<Notification>()
            .HasIndex(notification => new { notification.SourceMessageId, notification.UserId })
            .IsUnique()
            .HasFilter("[SourceMessageId] IS NOT NULL");

        modelBuilder.Entity<NewsletterSubscription>()
            .HasIndex(subscription => subscription.UserId)
            .IsUnique();

        modelBuilder.Entity<CandidateSkill>()
            .HasIndex(skill => new { skill.NormalizedName, skill.CandidateProfileId })
            .IsUnique();
        modelBuilder.Entity<CandidateSkill>()
            .HasOne(skill => skill.CandidateProfile)
            .WithMany()
            .HasForeignKey(skill => skill.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobReport>()
            .HasOne(report => report.JobPost)
            .WithMany()
            .HasForeignKey(report => report.JobPostId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<JobReport>()
            .HasOne(report => report.Reporter)
            .WithMany()
            .HasForeignKey(report => report.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<JobReport>()
            .HasIndex(report => new { report.Status, report.CreatedAt });
        modelBuilder.Entity<SavedJob>()
    .ToTable("SavedJobs");

        modelBuilder.Entity<SavedJob>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Tránh cascade vòng lặp

        modelBuilder.Entity<SavedJob>()
            .HasOne(s => s.JobPost)
            .WithMany()
            .HasForeignKey(s => s.JobPostId)
            .OnDelete(DeleteBehavior.Cascade); // Cái này OK vì không tạo vòng lặp
        modelBuilder.Entity<SavedJob>()
            .HasIndex(s => new { s.UserId, s.JobPostId })
            .IsUnique();

        modelBuilder.Entity<MatchResult>()
            .HasIndex(m => new { m.CandidateId, m.JobPostId })
            .IsUnique();
        modelBuilder.Entity<MatchResult>()
            .HasIndex(m => new { m.JobPostId, m.TotalScore });
        modelBuilder.Entity<MatchResult>()
            .HasIndex(m => new { m.CandidateId, m.TotalScore });
        modelBuilder.Entity<MatchResult>()
            .HasOne(m => m.Candidate)
            .WithMany()
            .HasForeignKey(m => m.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MatchResult>()
            .HasOne(m => m.JobPost)
            .WithMany()
            .HasForeignKey(m => m.JobPostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlanEntitlement>()
            .HasIndex(item => new { item.PlanId, item.CreditType })
            .IsUnique();
        modelBuilder.Entity<PlanEntitlement>()
            .HasOne(item => item.Plan)
            .WithMany(plan => plan.Entitlements)
            .HasForeignKey(item => item.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentOrder>()
            .HasIndex(order => order.VnpTxnRef)
            .IsUnique();
        modelBuilder.Entity<PaymentOrder>()
            .HasIndex(order => new { order.UserId, order.CreatedAt });
        modelBuilder.Entity<PaymentOrder>()
            .HasIndex(order => new { order.Status, order.ExpiresAt });
        modelBuilder.Entity<PaymentOrder>()
            .HasOne(order => order.User)
            .WithMany()
            .HasForeignKey(order => order.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PaymentOrder>()
            .HasOne(order => order.Plan)
            .WithMany(plan => plan.PaymentOrders)
            .HasForeignKey(order => order.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CreditLedger>()
            .HasIndex(entry => new { entry.PaymentOrderId, entry.CreditType })
            .IsUnique();
        modelBuilder.Entity<CreditLedger>()
            .HasIndex(entry => new { entry.UserId, entry.CreditType, entry.ExpiresAt });
        modelBuilder.Entity<CreditLedger>()
            .HasOne(entry => entry.User)
            .WithMany()
            .HasForeignKey(entry => entry.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CreditLedger>()
            .HasOne(entry => entry.PaymentOrder)
            .WithMany(order => order.CreditLedger)
            .HasForeignKey(entry => entry.PaymentOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        base.OnModelCreating(modelBuilder);

    }
}

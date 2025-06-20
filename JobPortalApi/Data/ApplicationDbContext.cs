using Microsoft.EntityFrameworkCore;
using JobPortalApi.Models;
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
    public DbSet<Company> Companies { get; set; }
    public DbSet<SavedJob> SavedJobs { get; set; }
    public DbSet<Category> Categories { get; set; } // Thêm DbSet<Category> nếu có
    public DbSet<CandidateProfile> candidateProfiles { get; set; } // Thêm DbSet<CategoryProfile> nếu có
    public DbSet<Notification> Notifications { get; set; } // Thêm DbSet<Notification> nếu có

    public DbSet<Review> Review { get; set; } // Thêm DbSet<Review> nếu có

    // Thêm các DbSet khác nếu có

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Map bảng JobPost thành "JobPosts"
        modelBuilder.Entity<JobPost>(entity =>
        {
            entity.ToTable("JobPosts");

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

        modelBuilder.Entity<Job>()
               .ToTable("Jobs"); // Map Job thành bảng Applies
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
        base.OnModelCreating(modelBuilder);

    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models
{

    /// Represents a system user.
    [Table("Users")]
    public class User
    {
        /// Unique identifier for the user.
        public Guid Id { get; set; } = Guid.NewGuid();

        /// Email address used for login.
        public string Email { get; set; }

        /// Hashed password using BCrypt.
        public string PasswordHash { get; set; }


        /// Full name of the user.
        public string FullName { get; set; }
        public DateTime CreatedAt { get; set; } // ✅ Dòng này phải tồn tại


        /// Role assigned to the user (Admin, Recruiter, or Candidate).
        // Entity Framework Core không yêu cầu tạo bảng riêng cho enum. Enum chỉ được ánh xạ vào một cột (column) trong bảng chứa nó
        public UserRole Role { get; set; } = UserRole.Candidate;
    }
}

using JobPortalApi.Models;
using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.DTOs.AdminUser
{
    public class CreateUserDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public UserRole Role { get; set; } // "Admin", "Recruiter", "Candidate"
    }
}

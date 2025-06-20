using JobPortalApi.Models;
using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.DTOs.AdminUser
{
    public class UpdateUserDto
    {
        [EmailAddress]
        public string Email { get; set; }

        public string FullName { get; set; }
        public UserRole? Role { get; set; }
    }

}

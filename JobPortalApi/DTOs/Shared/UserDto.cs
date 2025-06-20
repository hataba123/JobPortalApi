using JobPortalApi.Models;

namespace JobPortalApi.DTOs.shared
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public string FullName { get; set; }
    }

}

using JobPortalApi.DTOs.shared;

namespace JobPortalApi.DTOs.Shared
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public UserDto User { get; set; } // ✅ thêm

    }
}

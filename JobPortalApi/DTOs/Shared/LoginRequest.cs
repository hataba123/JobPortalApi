public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    // ✅ Thêm dòng này vào:
    public bool IsOAuth { get; set; } = false;
}

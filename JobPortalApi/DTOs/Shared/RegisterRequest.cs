using JobPortalApi.Models;

public class RegisterRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FullName { get; set; }
    public UserRole Role { get; set; } = UserRole.Candidate;
}

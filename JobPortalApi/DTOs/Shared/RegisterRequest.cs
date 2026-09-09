using System.ComponentModel.DataAnnotations;
using JobPortalApi.Models;

public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required, MinLength(8)]
    public string Password { get; set; }

    [Required]
    public string FullName { get; set; }
    public UserRole Role { get; set; } = UserRole.Candidate;
}

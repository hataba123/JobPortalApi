using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.DTOs.Shared;

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; }

    [Required, MinLength(8)]
    public string NewPassword { get; set; }
}

public class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; }
}

public class ResetPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Token { get; set; }

    [Required, MinLength(8)]
    public string NewPassword { get; set; }
}

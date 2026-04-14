using System.ComponentModel.DataAnnotations;

namespace TenantCore.Shared.Dtos.Auth;

/// <summary>
/// Request model for user registration.
/// </summary>
public class RegisterRequestDto
{
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Middle name cannot exceed 100 characters")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string EmailId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(256, ErrorMessage = "Password cannot exceed 256 characters")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Mobile number cannot exceed 20 characters")]
    public string? MobileNo { get; set; }
}

/// <summary>
/// Request model for user login (Step 1).
/// </summary>
public class LoginRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string EmailId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request model for 2FA validation (Step 2).
/// </summary>
public class ValidateTwoFactorRequestDto
{
    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP code must contain only digits")]
    public string OtpCode { get; set; } = string.Empty;
}

/// <summary>
/// Request model for password reset.
/// </summary>
public class ResetPasswordRequestDto
{
    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(256, ErrorMessage = "Password cannot exceed 256 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request model for accepting an invitation.
/// </summary>
public class AcceptInvitationRequestDto
{
    [Required(ErrorMessage = "Invitation token is required")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request model for resending email verification.
/// </summary>
public class ResendEmailVerificationRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string EmailId { get; set; } = string.Empty;
}

/// <summary>
/// Request model for disabling 2FA.
/// </summary>
public class DisableTwoFactorRequestDto
{
    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
    public string Code { get; set; } = string.Empty;
}

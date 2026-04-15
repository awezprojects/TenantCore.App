using System.ComponentModel.DataAnnotations;

namespace TenantCore.Shared.Dtos.Auth;

/// <summary>
/// Request model for creating or updating an application.
/// </summary>
public class ApplicationCreationRequestDto
{
    [Required(ErrorMessage = "Application name is required")]
    [MaxLength(200, ErrorMessage = "Application name cannot exceed 200 characters")]
    public string ApplicationName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Application code is required")]
    [MaxLength(50, ErrorMessage = "Application code cannot exceed 50 characters")]
    public string ApplicationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Application type is required")]
    public int ApplicationType { get; set; }

    [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? Address { get; set; }

    [MaxLength(20, ErrorMessage = "Contact number cannot exceed 20 characters")]
    public string? ContactNumber { get; set; }

    [MaxLength(200, ErrorMessage = "Contact person cannot exceed 200 characters")]
    public string? ContactPerson { get; set; }

    [MaxLength(100, ErrorMessage = "Registration number cannot exceed 100 characters")]
    public string? RegistrationNumber { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(256, ErrorMessage = "Official email cannot exceed 256 characters")]
    public string? OfficialEmail { get; set; }

    [Url(ErrorMessage = "Invalid URL format")]
    [MaxLength(500, ErrorMessage = "Website URL cannot exceed 500 characters")]
    public string? Website { get; set; }

    public string? AdditionalInfo { get; set; }
}

/// <summary>
/// Request model for inviting a user to an application.
/// </summary>
public class InviteUserRequestDto
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

    [MaxLength(20, ErrorMessage = "Mobile number cannot exceed 20 characters")]
    public string? MobileNo { get; set; }

    [Required(ErrorMessage = "Application ID is required")]
    public Guid ApplicationId { get; set; }

    [Required(ErrorMessage = "Role ID is required")]
    public Guid RoleId { get; set; }
}

/// <summary>
/// Request model for toggling active status.
/// </summary>
public class ToggleStatusRequestDto
{
    [Required(ErrorMessage = "IsActive is required")]
    public bool IsActive { get; set; }
}

/// <summary>
/// Request model for changing a user's role.
/// </summary>
public class ChangeUserRoleRequestDto
{
    [Required(ErrorMessage = "New role ID is required")]
    public Guid NewRoleId { get; set; }
}

/// <summary>
/// Request model for inviting an existing user to an application.
/// </summary>
public class InviteExistingUserRequestDto
{
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Application ID is required")]
    public Guid ApplicationId { get; set; }

    [Required(ErrorMessage = "Role ID is required")]
    public Guid RoleId { get; set; }
}

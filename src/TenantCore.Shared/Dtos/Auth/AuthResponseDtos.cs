namespace TenantCore.Shared.Dtos.Auth;

/// <summary>
/// Response model for login operations.
/// </summary>
public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserProfileDto? User { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public bool RequiresPasswordReset { get; set; }
    public List<ApplicationDto> AvailableApplications { get; set; } = [];

    // 2FA specific fields
    public string? TempLoginToken { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? UserDisplayName { get; set; }
    public bool IsFirstTimeSetup { get; set; }
}

/// <summary>
/// User profile response model.
/// </summary>
public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string EmailId { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public string? MobileNo { get; set; }
    public bool IsMobileVerified { get; set; }
    public bool IsActive { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public List<UserApplicationDto> UserApplications { get; set; } = [];
    public List<UserRoleDto> UserRoles { get; set; } = [];

    public string FullName => string.IsNullOrEmpty(MiddleName) 
        ? $"{FirstName} {LastName}" 
        : $"{FirstName} {MiddleName} {LastName}";
}

/// <summary>
/// User application association model.
/// </summary>
public class UserApplicationDto
{
    public Guid UserApplicationId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// User role association model.
/// </summary>
public class UserRoleDto
{
    public Guid UserRoleId { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Application details model.
/// </summary>
public class ApplicationDto
{
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string ApplicationCode { get; set; } = string.Empty;
    public int ApplicationType { get; set; }
    public bool IsActive { get; set; }
    public bool IsOwner { get; set; }
}

/// <summary>
/// Response model for enabling 2FA.
/// </summary>
public class EnableTwoFactorResponseDto
{
    public string? QrCodeBase64 { get; set; }
    public string? ManualEntryKey { get; set; }
    public bool RequiresOtpConfirmation { get; set; }
    public string? Message { get; set; }
}

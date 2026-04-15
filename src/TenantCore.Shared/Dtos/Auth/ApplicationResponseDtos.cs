namespace TenantCore.Shared.Dtos.Auth;

/// <summary>
/// Response model for an application.
/// </summary>
public class ApplicationResponseDto
{
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string ApplicationCode { get; set; } = string.Empty;
    public int ApplicationType { get; set; }
    public string? Address { get; set; }
    public string? ContactNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? OfficialEmail { get; set; }
    public string? Website { get; set; }
    public string? AdditionalInfo { get; set; }
    public bool IsOwner { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public List<UserRoleResponseDto> UserRoles { get; set; } = [];
}

/// <summary>
/// Response model for an application user with role information.
/// </summary>
public class ApplicationUserResponseDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string EmailId { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public bool IsActive { get; set; }
    public bool IsOwner { get; set; }
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public DateTime JoinedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// Response model for an invitation.
/// </summary>
public class InvitationResponseDto
{
    public Guid InvitationId { get; set; }
    public string EmailId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsAccepted { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Response model for a user-role assignment.
/// </summary>
public class UserRoleResponseDto
{
    public Guid UserRoleId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid ApplicationId { get; set; }
    public bool IsActive { get; set; }
}

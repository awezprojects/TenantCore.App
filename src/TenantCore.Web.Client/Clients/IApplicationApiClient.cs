using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

/// <summary>
/// Client interface for Application API operations.
/// </summary>
public interface IApplicationApiClient
{
    Task<ApiResponse<ApplicationResponseDto>> CreateApplicationAsync(Guid ownerId, ApplicationCreationRequestDto request);
    Task<ApiResponse<ApplicationResponseDto>> EditApplicationAsync(Guid applicationId, ApplicationCreationRequestDto request);
    Task<ApiResponse<ApplicationResponseDto>> GetApplicationByIdAsync(Guid applicationId);
    Task<ApiResponse<ApplicationResponseDto>> GetApplicationByCodeAsync(string code);
    Task<ApiResponse<List<ApplicationResponseDto>>> GetAllApplicationsAsync();
    Task<ApiResponse<List<ApplicationResponseDto>>> GetApplicationsByTypeAsync(int applicationType);
    Task<ApiResponse<List<ApplicationUserResponseDto>>> GetApplicationUsersAsync(Guid applicationId);
    Task<ApiResponse<InvitationResponseDto>> InviteUserAsync(Guid invitedBy, InviteUserRequestDto request);
    Task<ApiResponse> AssignUserToApplicationAsync(Guid applicationId, Guid userId, Guid roleId, Guid assignedBy);
    Task<ApiResponse> AddApplicationUserMappingAsync(Guid applicationId, Guid userId, Guid assignedBy);
    Task<ApiResponse> RemoveUserFromApplicationAsync(Guid applicationId, Guid userId, Guid removedBy);
    Task<ApiResponse> DeleteApplicationAsync(Guid applicationId);
    Task<ApiResponse<ApplicationRolesResponseDto>> GetRolesByApplicationAsync(Guid applicationId);
    Task<ApiResponse> ToggleApplicationStatusAsync(Guid applicationId, Guid modifiedBy, ToggleStatusRequestDto request);
    Task<ApiResponse> ToggleUserApplicationMappingAsync(Guid applicationId, Guid userId, Guid modifiedBy, ToggleStatusRequestDto request);
    Task<ApiResponse> ChangeUserRoleAsync(Guid applicationId, Guid userId, Guid modifiedBy, ChangeUserRoleRequestDto request);
    Task<ApiResponse> InviteExistingUserAsync(Guid invitedBy, InviteExistingUserRequestDto request);
    Task<ApiResponse<List<ApplicationUserResponseDto>>> GetDeactivatedApplicationUsersAsync(Guid applicationId);
}

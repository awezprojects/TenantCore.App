using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Services;

/// <summary>
/// Abstraction for the external TenantCore.Auth Application API.
/// Implementations are responsible for forwarding requests to the Auth service,
/// including forwarding the current user's bearer token.
/// </summary>
public interface IAuthApplicationService
{
    Task<ApplicationResponseDto> CreateApplicationAsync(Guid ownerId, ApplicationCreationRequestDto request, CancellationToken cancellationToken = default);
    Task<ApplicationResponseDto> EditApplicationAsync(Guid applicationId, ApplicationCreationRequestDto request, CancellationToken cancellationToken = default);
    Task<ApplicationResponseDto?> GetApplicationByIdAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<ApplicationResponseDto?> GetApplicationByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<ApplicationResponseDto>> GetAllApplicationsAsync(CancellationToken cancellationToken = default);
    Task<List<ApplicationResponseDto>> GetApplicationsByTypeAsync(int applicationType, CancellationToken cancellationToken = default);
    Task<List<ApplicationUserResponseDto>> GetApplicationUsersAsync(Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a minimal staff directory (userId/fullName/roleName only — no email/phone) for the application,
    /// optionally filtered to a set of role names. Unlike <see cref="GetApplicationUsersAsync"/>, Auth allows
    /// any active member of the application to call this — use it for non-admin-facing pickers
    /// (OPD doctor dropdown, handover recipients). See DEF-AUTH-OPD-01.
    /// </summary>
    Task<List<ClinicStaffDirectoryDto>> GetClinicStaffDirectoryAsync(Guid applicationId, IEnumerable<string>? roles, CancellationToken cancellationToken = default);
    Task<InvitationResponseDto> InviteUserAsync(Guid invitedBy, InviteUserRequestDto request, CancellationToken cancellationToken = default);
    Task AssignUserToApplicationAsync(Guid applicationId, Guid userId, Guid roleId, Guid assignedBy, CancellationToken cancellationToken = default);
    Task AddApplicationUserMappingAsync(Guid applicationId, Guid userId, Guid assignedBy, CancellationToken cancellationToken = default);
    Task RemoveUserFromApplicationAsync(Guid applicationId, Guid userId, Guid removedBy, CancellationToken cancellationToken = default);
    Task DeleteApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task ToggleApplicationStatusAsync(Guid applicationId, Guid modifiedBy, bool isActive, CancellationToken cancellationToken = default);
    Task ToggleUserApplicationMappingAsync(Guid applicationId, Guid userId, Guid modifiedBy, bool isActive, CancellationToken cancellationToken = default);
    Task ChangeUserRoleAsync(Guid applicationId, Guid userId, Guid modifiedBy, Guid newRoleId, CancellationToken cancellationToken = default);
    Task InviteExistingUserAsync(Guid invitedBy, InviteExistingUserRequestDto request, CancellationToken cancellationToken = default);
    Task<List<ApplicationUserResponseDto>> GetDeactivatedApplicationUsersAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<List<InvitationResponseDto>> GetApplicationInvitationsAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task ReinviteUserAsync(Guid applicationId, Guid invitationId, Guid reinvitedBy, CancellationToken cancellationToken = default);
    Task DeleteInvitationAsync(Guid applicationId, Guid invitationId, CancellationToken cancellationToken = default);
}

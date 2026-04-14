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
    Task<InvitationResponseDto> InviteUserAsync(Guid invitedBy, InviteUserRequestDto request, CancellationToken cancellationToken = default);
    Task AssignUserToApplicationAsync(Guid applicationId, Guid userId, Guid roleId, Guid assignedBy, CancellationToken cancellationToken = default);
    Task AddApplicationUserMappingAsync(Guid applicationId, Guid userId, Guid assignedBy, CancellationToken cancellationToken = default);
    Task RemoveUserFromApplicationAsync(Guid applicationId, Guid userId, Guid removedBy, CancellationToken cancellationToken = default);
    Task DeleteApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
}

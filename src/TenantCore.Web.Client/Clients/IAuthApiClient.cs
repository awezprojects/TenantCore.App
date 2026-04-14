using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

/// <summary>
/// Client interface for Auth API operations.
/// </summary>
public interface IAuthApiClient
{
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResponse<LoginResponseDto>> ValidateTwoFactorAsync(string tempToken, ValidateTwoFactorRequestDto request);
    Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync();
    Task<ApiResponse> LogoutAsync();
    Task<ApiResponse> LogoutAllSessionsAsync();
    Task<ApiResponse<UserProfileDto>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResponse> VerifyEmailAsync(Guid userId, string verificationCode);
    Task<ApiResponse> ResendEmailVerificationAsync(ResendEmailVerificationRequestDto request);
    Task<ApiResponse> ResetPasswordAsync(Guid userId, ResetPasswordRequestDto request);
    Task<ApiResponse<UserProfileDto>> AcceptInvitationAsync(AcceptInvitationRequestDto request);
    Task<ApiResponse<UserProfileDto>> GetUserByIdAsync(Guid userId);
    Task<ApiResponse> DisableTwoFactorAsync(Guid userId, DisableTwoFactorRequestDto request);
}

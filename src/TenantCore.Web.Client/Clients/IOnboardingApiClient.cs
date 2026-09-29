using TenantCore.Shared.Dtos.Auth;
using TenantCore.Shared.Dtos.Onboarding;

namespace TenantCore.Web.Client.Clients;

public interface IOnboardingApiClient
{
    Task<ApiResponse<Guid>> SubmitAsync(SubmitClinicOnboardingRequest request);
    Task<ApiResponse<IEnumerable<ClinicOnboardingRequestDto>>> GetMineAsync();
    Task<ApiResponse<ClinicOnboardingRequestDto>> GetByIdAsync(Guid id);
    Task<ApiResponse> CancelAsync(Guid id);
    Task<ApiResponse> CheckPaymentAsync(Guid id);
}

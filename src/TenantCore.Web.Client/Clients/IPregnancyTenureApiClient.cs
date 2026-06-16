using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IPregnancyTenureApiClient
{
    Task<ApiResponse<IEnumerable<PregnancyTenureSummaryDto>>> GetOverdueAsync();
    Task<ApiResponse<PregnancyTenureDto?>> GetActiveForPatientAsync(Guid patientId);
    Task<ApiResponse<IEnumerable<PregnancyTenureDto>>> GetAllForPatientAsync(Guid patientId);
    Task<ApiResponse<PregnancyTenureDto>> CloseAsync(Guid tenureId, ClosePregnancyTenureRequest request);
}

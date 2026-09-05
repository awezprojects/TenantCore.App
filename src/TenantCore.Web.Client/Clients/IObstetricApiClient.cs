using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IObstetricApiClient
{
    Task<ApiResponse<ObstetricDatesDto>> GetObstetricDatesAsync(Guid prescriptionId);
    Task<ApiResponse<UsgChartDto>> GetUsgChartAsync(Guid patientId);
    Task<ApiResponse<ObstetricDatesDto>> SetLmpAsync(Guid prescriptionId, SetLmpRequest request);
    Task<ApiResponse<ObstetricDatesDto>> SetEddByUsgAsync(Guid prescriptionId, SetEddByUsgRequest request);
    Task<ApiResponse<IEnumerable<HistoryLookupItemDto>>> GetHistoryLookupItemsAsync();
    Task<ApiResponse<HistoryLookupItemDto>> AddHistoryLookupItemAsync(AddHistoryLookupItemDto dto);
}

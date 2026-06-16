using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IUsgTemplateApiClient
{
    Task<ApiResponse<ClinicUsgTemplateDto>> GetClinicTemplateAsync();
    Task<ApiResponse<ClinicUsgTemplateDto>> GetDefaultTemplateAsync();
    Task<ApiResponse<ClinicUsgTemplateDto>> UpsertAsync(UpsertClinicUsgTemplateRequest request);
    Task<ApiResponse<ClinicUsgTemplateDto>> ResetAsync();
}

using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Web.Client.Clients;

public interface ITenantApiClient
{
    Task<PagedResult<TenantDto>> GetTenantsAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<TenantDto?> GetTenantByIdAsync(Guid id);
    Task<TenantDto> CreateTenantAsync(CreateTenantDto dto);
    Task<TenantDto> UpdateTenantAsync(Guid id, UpdateTenantDto dto);
    Task DeleteTenantAsync(Guid id);
}

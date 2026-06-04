using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Services;

public interface IAuthClinicService
{
    Task<ApplicationResponseDto> CreateClinicAsync(CreateClinicRequestDto request, CancellationToken ct = default);
    Task<List<ClinicDashboardItemDto>> GetClinicDashboardAsync(CancellationToken ct = default);
}

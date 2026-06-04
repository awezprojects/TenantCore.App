using MediatR;
using TenantCore.Application.Features.Clinics.Queries;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Clinics.Handlers;

public sealed class GetClinicDashboardHandler(IAuthClinicService clinicService)
    : IRequestHandler<GetClinicDashboardQuery, List<ClinicDashboardItemDto>>
{
    public async Task<List<ClinicDashboardItemDto>> Handle(GetClinicDashboardQuery request, CancellationToken cancellationToken)
        => await clinicService.GetClinicDashboardAsync(cancellationToken);
}

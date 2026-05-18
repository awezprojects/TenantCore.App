using MediatR;
using TenantCore.Application.Features.Prescriptions.Queries;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Prescriptions.Handlers;

public sealed class GetDoctorOpdCountHandler(IOpdRegistrationRepository opdRepository)
    : IRequestHandler<GetDoctorOpdCountQuery, int>
{
    public async Task<int> Handle(GetDoctorOpdCountQuery request, CancellationToken cancellationToken)
        => await opdRepository.CountTodayByDoctorAsync(request.ApplicationId, request.DoctorUserId, cancellationToken);
}

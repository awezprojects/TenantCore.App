using MediatR;
using TenantCore.Application.Features.Clinics.Commands;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Clinics.Handlers;

public sealed class CreateClinicHandler(IAuthClinicService clinicService)
    : IRequestHandler<CreateClinicCommand, ApplicationResponseDto>
{
    public async Task<ApplicationResponseDto> Handle(CreateClinicCommand request, CancellationToken cancellationToken)
        => await clinicService.CreateClinicAsync(request.Request, cancellationToken);
}

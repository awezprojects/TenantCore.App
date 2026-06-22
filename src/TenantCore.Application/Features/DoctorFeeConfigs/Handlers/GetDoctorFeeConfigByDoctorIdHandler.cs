using MediatR;
using TenantCore.Application.Features.DoctorFeeConfigs.Queries;
using TenantCore.Application.Features.DoctorFeeConfigs.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Handlers;

public sealed class GetDoctorFeeConfigByDoctorIdHandler(IDoctorFeeConfigRepository repository) : IRequestHandler<GetDoctorFeeConfigByDoctorIdQuery, DoctorFeeConfigDto?>
{
    public async Task<DoctorFeeConfigDto?> Handle(GetDoctorFeeConfigByDoctorIdQuery request, CancellationToken cancellationToken)
    {
        var config = await repository.GetByDoctorProfileIdAsync(request.DoctorProfileId, request.ApplicationId, cancellationToken);
        return config is null ? null : DoctorFeeConfigTranslator.ToDto(config);
    }
}

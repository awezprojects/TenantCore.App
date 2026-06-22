using MediatR;
using TenantCore.Application.Features.DoctorFeeConfigs.Queries;
using TenantCore.Application.Features.DoctorFeeConfigs.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Handlers;

public sealed class GetDoctorFeeConfigByIdHandler(IDoctorFeeConfigRepository repository) : IRequestHandler<GetDoctorFeeConfigByIdQuery, DoctorFeeConfigDto>
{
    public async Task<DoctorFeeConfigDto> Handle(GetDoctorFeeConfigByIdQuery request, CancellationToken cancellationToken)
    {
        var config = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (config is null || config.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(DoctorFeeConfig), request.Id);

        return DoctorFeeConfigTranslator.ToDto(config);
    }
}

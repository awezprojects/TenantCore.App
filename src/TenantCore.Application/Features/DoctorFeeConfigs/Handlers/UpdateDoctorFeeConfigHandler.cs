using MediatR;
using TenantCore.Application.Features.DoctorFeeConfigs.Commands;
using TenantCore.Application.Features.DoctorFeeConfigs.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Handlers;

public sealed class UpdateDoctorFeeConfigHandler(IDoctorFeeConfigRepository repository) : IRequestHandler<UpdateDoctorFeeConfigCommand, DoctorFeeConfigDto>
{
    public async Task<DoctorFeeConfigDto> Handle(UpdateDoctorFeeConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (config is null || config.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(DoctorFeeConfig), request.Id);

        config.Update(request.Request.VisitFee, request.Request.IsActive);
        repository.Update(config);
        await repository.SaveChangesAsync(cancellationToken);
        return DoctorFeeConfigTranslator.ToDto(config);
    }
}

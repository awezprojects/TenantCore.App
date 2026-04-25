using MediatR;
using TenantCore.Application.Features.ClinicSettings.Commands;
using TenantCore.Application.Features.ClinicSettings.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Handlers;

public sealed class UpdateClinicFeeConfigHandler(IClinicFeeConfigRepository repository)
    : IRequestHandler<UpdateClinicFeeConfigCommand, ClinicFeeConfigDto>
{
    public async Task<ClinicFeeConfigDto> Handle(
        UpdateClinicFeeConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await repository.GetByApplicationAsync(request.ApplicationId, cancellationToken);

        if (config is null)
        {
            config = ClinicFeeConfig.Create(request.ApplicationId, request.OpdFee);
            await repository.AddAsync(config, cancellationToken);
        }
        else
        {
            config.Update(request.OpdFee);
            repository.Update(config);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return ClinicFeeConfigTranslator.ToDto(config);
    }
}

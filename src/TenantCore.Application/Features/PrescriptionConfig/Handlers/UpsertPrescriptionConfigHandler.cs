using MediatR;
using TenantCore.Application.Features.PrescriptionConfig.Commands;
using TenantCore.Application.Features.PrescriptionConfig.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using DomainConfig = TenantCore.Domain.Entities.PrescriptionConfig;

namespace TenantCore.Application.Features.PrescriptionConfig.Handlers;

public sealed class UpsertPrescriptionConfigHandler(IPrescriptionConfigRepository repository)
    : IRequestHandler<UpsertPrescriptionConfigCommand, PrescriptionConfigDto>
{
    public async Task<PrescriptionConfigDto> Handle(
        UpsertPrescriptionConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await repository.GetByApplicationIdAsync(request.ApplicationId, cancellationToken);

        if (config is null)
        {
            config = DomainConfig.Create(
                request.ApplicationId, request.DefaultLanguage,
                request.PrintMarginTop, request.PrintMarginRight,
                request.PrintMarginBottom, request.PrintMarginLeft,
                request.HideClinicHeader);
            await repository.AddAsync(config, cancellationToken);
        }
        else
        {
            config.Update(
                request.DefaultLanguage,
                request.PrintMarginTop, request.PrintMarginRight,
                request.PrintMarginBottom, request.PrintMarginLeft,
                request.HideClinicHeader);
            repository.Update(config);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return PrescriptionConfigTranslator.ToDto(config);
    }
}

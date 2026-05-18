using MediatR;
using TenantCore.Application.Features.PrescriptionConfig.Queries;
using TenantCore.Application.Features.PrescriptionConfig.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.PrescriptionConfig.Handlers;

public sealed class GetPrescriptionConfigHandler(IPrescriptionConfigRepository repository)
    : IRequestHandler<GetPrescriptionConfigQuery, PrescriptionConfigDto>
{
    public async Task<PrescriptionConfigDto> Handle(
        GetPrescriptionConfigQuery request, CancellationToken cancellationToken)
    {
        var config = await repository.GetByApplicationIdAsync(request.ApplicationId, cancellationToken);

        if (config is null)
            return new PrescriptionConfigDto
            {
                ApplicationId = request.ApplicationId,
                DefaultLanguage = PrescriptionLanguage.English
            };

        return PrescriptionConfigTranslator.ToDto(config);
    }
}

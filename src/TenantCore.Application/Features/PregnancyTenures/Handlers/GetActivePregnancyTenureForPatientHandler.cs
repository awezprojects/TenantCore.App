using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.PregnancyTenures.Queries;
using TenantCore.Application.Features.PregnancyTenures.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.PregnancyTenures.Handlers;

public sealed class GetActivePregnancyTenureForPatientHandler(
    IPregnancyTenureRepository repository,
    ILogger<GetActivePregnancyTenureForPatientHandler> logger)
    : IRequestHandler<GetActivePregnancyTenureForPatientQuery, PregnancyTenureDto?>
{
    public async Task<PregnancyTenureDto?> Handle(GetActivePregnancyTenureForPatientQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active pregnancy tenure for patient {PatientId}", request.PatientId);

        var tenure = await repository.GetActiveForPatientAsync(request.PatientId, request.ApplicationId, cancellationToken);

        return tenure is null ? null : PregnancyTenureTranslator.ToDto(tenure);
    }
}

using MediatR;
using TenantCore.Application.Features.PregnancyTenures.Queries;
using TenantCore.Application.Features.PregnancyTenures.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.PregnancyTenures.Handlers;

public sealed class GetPatientPregnancyTenuresHandler(IPregnancyTenureRepository repository)
    : IRequestHandler<GetPatientPregnancyTenuresQuery, IEnumerable<PregnancyTenureDto>>
{
    public async Task<IEnumerable<PregnancyTenureDto>> Handle(
        GetPatientPregnancyTenuresQuery request, CancellationToken cancellationToken)
    {
        var tenures = await repository.GetAllForPatientAsync(
            request.PatientId, request.ApplicationId, cancellationToken);

        return tenures.Select(PregnancyTenureTranslator.ToDto);
    }
}

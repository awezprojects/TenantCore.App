using MediatR;
using TenantCore.Application.Features.ClinicSettings.Queries;
using TenantCore.Application.Features.ClinicSettings.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Handlers;

public sealed class GetCitiesByStateHandler(ICityRepository repository)
    : IRequestHandler<GetCitiesByStateQuery, IEnumerable<CityDto>>
{
    public async Task<IEnumerable<CityDto>> Handle(GetCitiesByStateQuery request, CancellationToken cancellationToken)
    {
        var cities = await repository.GetByStateIdAsync(request.StateId, cancellationToken);
        return cities.Select(LocationTranslator.ToDto);
    }
}

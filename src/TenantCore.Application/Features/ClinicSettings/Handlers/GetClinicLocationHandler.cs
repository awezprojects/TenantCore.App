using MediatR;
using TenantCore.Application.Features.ClinicSettings.Queries;
using TenantCore.Application.Features.ClinicSettings.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Handlers;

public sealed class GetClinicLocationHandler(IClinicLocationRepository repository)
    : IRequestHandler<GetClinicLocationQuery, ClinicLocationDto?>
{
    public async Task<ClinicLocationDto?> Handle(GetClinicLocationQuery request, CancellationToken cancellationToken)
    {
        var location = await repository.GetByApplicationAsync(request.ApplicationId, cancellationToken);
        return location is null ? null : LocationTranslator.ToDto(location);
    }
}

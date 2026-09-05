using MediatR;
using TenantCore.Application.Features.ClinicSettings.Commands;
using TenantCore.Application.Features.ClinicSettings.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Handlers;

public sealed class UpsertClinicLocationHandler(
    IClinicLocationRepository repository,
    ICityRepository cityRepository)
    : IRequestHandler<UpsertClinicLocationCommand, ClinicLocationDto>
{
    public async Task<ClinicLocationDto> Handle(UpsertClinicLocationCommand request, CancellationToken cancellationToken)
    {
        var cityBelongsToState = await cityRepository.ExistsForStateAsync(request.CityId, request.StateId, cancellationToken);
        if (!cityBelongsToState)
            throw new DomainValidationException("The selected city does not belong to the selected state.");

        var existing = await repository.GetByApplicationAsync(request.ApplicationId, cancellationToken);
        if (existing is not null)
        {
            existing.Update(request.StateId, request.CityId);
            repository.Update(existing);
        }
        else
        {
            existing = ClinicLocation.Create(request.ApplicationId, request.StateId, request.CityId);
            await repository.AddAsync(existing, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);

        var saved = await repository.GetByApplicationAsync(request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(ClinicLocation), request.ApplicationId);
        return LocationTranslator.ToDto(saved);
    }
}

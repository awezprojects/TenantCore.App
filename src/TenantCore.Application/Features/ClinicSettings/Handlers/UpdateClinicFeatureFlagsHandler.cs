using MediatR;
using TenantCore.Application.Features.ClinicSettings.Commands;
using TenantCore.Application.Features.ClinicSettings.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Handlers;

public sealed class UpdateClinicFeatureFlagsHandler(IClinicFeatureFlagsRepository repository)
    : IRequestHandler<UpdateClinicFeatureFlagsCommand, ClinicFeatureFlagsDto>
{
    public async Task<ClinicFeatureFlagsDto> Handle(
        UpdateClinicFeatureFlagsCommand request, CancellationToken cancellationToken)
    {
        var flags = await repository.GetByApplicationAsync(request.ApplicationId, cancellationToken);

        if (flags is null)
        {
            flags = ClinicFeatureFlags.Create(request.ApplicationId, request.PrepaidOpdEnabled);
            await repository.AddAsync(flags, cancellationToken);
        }
        else
        {
            flags.Update(request.PrepaidOpdEnabled);
            repository.Update(flags);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return ClinicFeatureFlagsTranslator.ToDto(flags);
    }
}

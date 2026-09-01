using MediatR;
using TenantCore.Application.Features.ClinicSettings.Queries;
using TenantCore.Application.Features.ClinicSettings.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Handlers;

public sealed class GetClinicFeatureFlagsHandler(IClinicFeatureFlagsRepository repository)
    : IRequestHandler<GetClinicFeatureFlagsQuery, ClinicFeatureFlagsDto?>
{
    public async Task<ClinicFeatureFlagsDto?> Handle(
        GetClinicFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        var flags = await repository.GetByApplicationAsync(request.ApplicationId, cancellationToken);
        return flags is null ? null : ClinicFeatureFlagsTranslator.ToDto(flags);
    }
}

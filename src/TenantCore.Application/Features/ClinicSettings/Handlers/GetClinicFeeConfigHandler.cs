using MediatR;
using TenantCore.Application.Features.ClinicSettings.Queries;
using TenantCore.Application.Features.ClinicSettings.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Handlers;

public sealed class GetClinicFeeConfigHandler(IClinicFeeConfigRepository repository)
    : IRequestHandler<GetClinicFeeConfigQuery, ClinicFeeConfigDto?>
{
    public async Task<ClinicFeeConfigDto?> Handle(
        GetClinicFeeConfigQuery request, CancellationToken cancellationToken)
    {
        var config = await repository.GetByApplicationAsync(request.ApplicationId, cancellationToken);
        return config is null ? null : ClinicFeeConfigTranslator.ToDto(config);
    }
}

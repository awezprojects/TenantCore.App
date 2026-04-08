using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Tenants.Queries;
using TenantCore.Application.Features.Tenants.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Tenants.Handlers;

public sealed class GetTenantByIdHandler(
    ITenantRepository repository,
    ILogger<GetTenantByIdHandler> logger)
    : IRequestHandler<GetTenantByIdQuery, TenantDto?>
{
    public async Task<TenantDto?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting tenant by id {TenantId}", request.Id);
        var tenant = await repository.GetByIdAsync(request.Id, cancellationToken);
        return tenant is null ? null : TenantTranslator.ToDto(tenant);
    }
}

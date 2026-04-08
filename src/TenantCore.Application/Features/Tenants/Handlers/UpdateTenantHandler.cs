using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Tenants.Commands;
using TenantCore.Application.Features.Tenants.Translators;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Tenants.Handlers;

public sealed class UpdateTenantHandler(
    ITenantRepository repository,
    ILogger<UpdateTenantHandler> logger)
    : IRequestHandler<UpdateTenantCommand, TenantDto>
{
    public async Task<TenantDto> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating tenant {TenantId}", request.Id);

        var tenant = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Tenant), request.Id);

        tenant.Update(
            request.Name,
            request.Subdomain,
            request.Description,
            request.ContactEmail,
            request.ContactPhone,
            request.SubscriptionExpiresAt);

        repository.Update(tenant);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Tenant updated {TenantId}", tenant.Id);
        return TenantTranslator.ToDto(tenant);
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Tenants.Commands;
using TenantCore.Application.Features.Tenants.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Tenants.Handlers;

public sealed class CreateTenantHandler(
    ITenantRepository repository,
    ILogger<CreateTenantHandler> logger)
    : IRequestHandler<CreateTenantCommand, TenantDto>
{
    public async Task<TenantDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating tenant {TenantName}", request.Name);

        var existing = await repository.GetBySubdomainAsync(request.Subdomain, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A tenant with subdomain '{request.Subdomain}' already exists.");

        var tenant = Tenant.Create(
            request.Name,
            request.Subdomain,
            request.Description,
            request.ContactEmail,
            request.ContactPhone,
            request.SubscriptionExpiresAt);

        await repository.AddAsync(tenant, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Tenant created with id {TenantId}", tenant.Id);
        return TenantTranslator.ToDto(tenant);
    }
}

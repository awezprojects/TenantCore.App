using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Tenants.Commands;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Tenants.Handlers;

public sealed class DeleteTenantHandler(
    ITenantRepository repository,
    ILogger<DeleteTenantHandler> logger)
    : IRequestHandler<DeleteTenantCommand>
{
    public async Task Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting tenant {TenantId}", request.Id);
        var tenant = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Tenant), request.Id);

        repository.Delete(tenant);
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Tenant deleted {TenantId}", request.Id);
    }
}

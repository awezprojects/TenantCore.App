using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.MedicineBundles.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.MedicineBundles.Handlers;

public sealed class DeleteMedicineBundleHandler(
    IMedicineBundleRepository repository,
    ILogger<DeleteMedicineBundleHandler> logger)
    : IRequestHandler<DeleteMedicineBundleCommand>
{
    public async Task Handle(DeleteMedicineBundleCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting medicine bundle {Id}", request.Id);

        var bundle = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MedicineBundle), request.Id);

        if (bundle.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(MedicineBundle), request.Id);

        repository.Delete(bundle);
        await repository.SaveChangesAsync(cancellationToken);
    }
}

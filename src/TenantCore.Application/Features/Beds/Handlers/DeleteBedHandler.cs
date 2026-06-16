using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Beds.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Beds.Handlers;

public sealed class DeleteBedHandler(IBedRepository repository, IApplicationAccessValidator accessValidator) : IRequestHandler<DeleteBedCommand>
{
    public async Task Handle(DeleteBedCommand request, CancellationToken cancellationToken)
    {
        var bed = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Bed), request.Id);

        if (!accessValidator.CanAccess(bed.ApplicationId))
            throw new NotFoundException(nameof(Bed), request.Id);

        if (bed.IsOccupied)
            throw new DomainValidationException("Cannot delete a bed that is currently occupied.");

        repository.Delete(bed);
        await repository.SaveChangesAsync(cancellationToken);
    }
}

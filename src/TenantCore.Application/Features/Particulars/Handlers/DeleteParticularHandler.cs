using MediatR;
using TenantCore.Application.Features.Particulars.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Particulars.Handlers;

public sealed class DeleteParticularHandler(IParticularRepository repository) : IRequestHandler<DeleteParticularCommand>
{
    public async Task Handle(DeleteParticularCommand request, CancellationToken cancellationToken)
    {
        var particular = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (particular is null || particular.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Particular), request.Id);

        repository.Delete(particular);
        await repository.SaveChangesAsync(cancellationToken);
    }
}

using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Wards.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Wards.Handlers;

public sealed class DeleteWardHandler(IWardRepository repository, IApplicationAccessValidator accessValidator) : IRequestHandler<DeleteWardCommand>
{
    public async Task Handle(DeleteWardCommand request, CancellationToken cancellationToken)
    {
        var ward = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Ward), request.Id);

        if (!accessValidator.CanAccess(ward.ApplicationId))
            throw new NotFoundException(nameof(Ward), request.Id);

        repository.Delete(ward);
        await repository.SaveChangesAsync(cancellationToken);
    }
}

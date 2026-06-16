using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.DosageRemarks.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.DosageRemarks.Handlers;

public sealed class DeleteDosageRemarkHandler(IDosageRemarkRepository repository, IApplicationAccessValidator accessValidator)
    : IRequestHandler<DeleteDosageRemarkCommand>
{
    public async Task Handle(DeleteDosageRemarkCommand request, CancellationToken cancellationToken)
    {
        var remark = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DosageRemark), request.Id);

        if (!accessValidator.CanAccess(remark.ApplicationId))
            throw new NotFoundException(nameof(DosageRemark), request.Id);

        repository.Delete(remark);
        await repository.SaveChangesAsync(cancellationToken);
    }
}

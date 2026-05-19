using MediatR;
using TenantCore.Application.Features.MedicineDosageForms.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.MedicineDosageForms.Handlers;

public sealed class DeleteMedicineDosageFormHandler(IMedicineDosageFormRepository repository)
    : IRequestHandler<DeleteMedicineDosageFormCommand>
{
    public async Task Handle(DeleteMedicineDosageFormCommand request, CancellationToken cancellationToken)
    {
        var form = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MedicineDosageForm), request.Id);

        form.Deactivate();
        repository.Update(form);
        await repository.SaveChangesAsync(cancellationToken);
    }
}

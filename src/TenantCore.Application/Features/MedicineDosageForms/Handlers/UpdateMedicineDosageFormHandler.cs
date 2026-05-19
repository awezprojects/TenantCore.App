using MediatR;
using TenantCore.Application.Features.MedicineDosageForms.Commands;
using TenantCore.Application.Features.MedicineDosageForms.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineDosageForms.Handlers;

public sealed class UpdateMedicineDosageFormHandler(IMedicineDosageFormRepository repository)
    : IRequestHandler<UpdateMedicineDosageFormCommand, MedicineDosageFormDto>
{
    public async Task<MedicineDosageFormDto> Handle(UpdateMedicineDosageFormCommand request, CancellationToken cancellationToken)
    {
        var form = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MedicineDosageForm), request.Id);

        form.Update(request.Name, request.Description, request.IsActive);
        repository.Update(form);
        await repository.SaveChangesAsync(cancellationToken);

        return MedicineDosageFormTranslator.ToDto(form);
    }
}

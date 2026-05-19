using MediatR;
using TenantCore.Application.Features.MedicineDosageForms.Commands;
using TenantCore.Application.Features.MedicineDosageForms.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineDosageForms.Handlers;

public sealed class CreateMedicineDosageFormHandler(IMedicineDosageFormRepository repository)
    : IRequestHandler<CreateMedicineDosageFormCommand, MedicineDosageFormDto>
{
    public async Task<MedicineDosageFormDto> Handle(CreateMedicineDosageFormCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByNameAsync(request.Name, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"Dosage form '{request.Name}' already exists.");

        var form = MedicineDosageForm.Create(request.Name, request.Description);
        await repository.AddAsync(form, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return MedicineDosageFormTranslator.ToDto(form);
    }
}

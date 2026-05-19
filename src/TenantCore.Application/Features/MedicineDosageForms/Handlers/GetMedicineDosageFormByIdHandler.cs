using MediatR;
using TenantCore.Application.Features.MedicineDosageForms.Queries;
using TenantCore.Application.Features.MedicineDosageForms.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineDosageForms.Handlers;

public sealed class GetMedicineDosageFormByIdHandler(IMedicineDosageFormRepository repository)
    : IRequestHandler<GetMedicineDosageFormByIdQuery, MedicineDosageFormDto>
{
    public async Task<MedicineDosageFormDto> Handle(GetMedicineDosageFormByIdQuery request, CancellationToken cancellationToken)
    {
        var form = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MedicineDosageForm), request.Id);

        return MedicineDosageFormTranslator.ToDto(form);
    }
}

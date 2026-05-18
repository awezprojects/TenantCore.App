using MediatR;
using TenantCore.Application.Features.DosageRemarks.Queries;
using TenantCore.Application.Features.DosageRemarks.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DosageRemarks.Handlers;

public sealed class GetDosageRemarkByIdHandler(IDosageRemarkRepository repository)
    : IRequestHandler<GetDosageRemarkByIdQuery, DosageRemarkDto>
{
    public async Task<DosageRemarkDto> Handle(GetDosageRemarkByIdQuery request, CancellationToken cancellationToken)
    {
        var remark = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DosageRemark), request.Id);

        if (remark.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(DosageRemark), request.Id);

        return DosageRemarkTranslator.ToDto(remark);
    }
}

using MediatR;
using TenantCore.Application.Features.DosageRemarks.Commands;
using TenantCore.Application.Features.DosageRemarks.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DosageRemarks.Handlers;

public sealed class UpdateDosageRemarkHandler(IDosageRemarkRepository repository)
    : IRequestHandler<UpdateDosageRemarkCommand, DosageRemarkDto>
{
    public async Task<DosageRemarkDto> Handle(UpdateDosageRemarkCommand request, CancellationToken cancellationToken)
    {
        var remark = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DosageRemark), request.Id);

        if (remark.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(DosageRemark), request.Id);

        remark.Update(request.MedicineForm, request.RemarkEnglish, request.RemarkHindi, request.RemarkMarathi, request.IsActive);
        repository.Update(remark);
        await repository.SaveChangesAsync(cancellationToken);

        return DosageRemarkTranslator.ToDto(remark);
    }
}

using MediatR;
using TenantCore.Application.Features.DosageRemarks.Commands;
using TenantCore.Application.Features.DosageRemarks.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DosageRemarks.Handlers;

public sealed class CreateDosageRemarkHandler(IDosageRemarkRepository repository)
    : IRequestHandler<CreateDosageRemarkCommand, DosageRemarkDto>
{
    public async Task<DosageRemarkDto> Handle(CreateDosageRemarkCommand request, CancellationToken cancellationToken)
    {
        var remark = DosageRemark.Create(
            request.ApplicationId,
            request.MedicineForm,
            request.RemarkEnglish,
            request.RemarkHindi,
            request.RemarkMarathi);

        await repository.AddAsync(remark, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return DosageRemarkTranslator.ToDto(remark);
    }
}

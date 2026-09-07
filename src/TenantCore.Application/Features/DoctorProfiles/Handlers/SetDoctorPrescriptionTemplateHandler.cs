using MediatR;
using TenantCore.Application.Features.DoctorProfiles.Commands;
using TenantCore.Application.Features.DoctorProfiles.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorProfiles.Handlers;

public sealed class SetDoctorPrescriptionTemplateHandler(IDoctorProfileRepository repository)
    : IRequestHandler<SetDoctorPrescriptionTemplateCommand, DoctorProfileDto>
{
    public async Task<DoctorProfileDto> Handle(SetDoctorPrescriptionTemplateCommand request, CancellationToken cancellationToken)
    {
        var profile = await repository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(DoctorProfile), request.UserId);

        profile.SetPreferredPrescriptionTemplate(request.Template);
        repository.Update(profile);
        await repository.SaveChangesAsync(cancellationToken);

        return DoctorProfileTranslator.ToDto(profile);
    }
}

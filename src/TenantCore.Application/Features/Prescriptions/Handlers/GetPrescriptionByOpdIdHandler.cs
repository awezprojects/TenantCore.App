using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Prescriptions.Queries;
using TenantCore.Application.Features.Prescriptions.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Handlers;

public sealed class GetPrescriptionByOpdIdHandler(
    IPrescriptionRepository prescriptionRepository,
    IPatientRepository patientRepository,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<GetPrescriptionByOpdIdQuery, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(GetPrescriptionByOpdIdQuery request, CancellationToken cancellationToken)
    {
        var prescription = await prescriptionRepository.GetByOpdRegistrationIdAsync(request.OpdRegistrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Prescription), request.OpdRegistrationId);

        if (!accessValidator.CanAccess(prescription.ApplicationId))
            throw new NotFoundException(nameof(Prescription), request.OpdRegistrationId);

        var patient = await patientRepository.GetByIdAsync(prescription.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), prescription.PatientId);

        return PrescriptionTranslator.ToDto(prescription, patient);
    }
}

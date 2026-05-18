using MediatR;
using TenantCore.Application.Features.Prescriptions.Queries;
using TenantCore.Application.Features.Prescriptions.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Handlers;

public sealed class GetPrescriptionByIdHandler(
    IPrescriptionRepository prescriptionRepository,
    IPatientRepository patientRepository)
    : IRequestHandler<GetPrescriptionByIdQuery, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(GetPrescriptionByIdQuery request, CancellationToken cancellationToken)
    {
        var prescription = await prescriptionRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Prescription), request.Id);

        if (prescription.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Prescription), request.Id);

        var patient = await patientRepository.GetByIdAsync(prescription.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), prescription.PatientId);

        return PrescriptionTranslator.ToDto(prescription, patient);
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Prescriptions.Commands;
using TenantCore.Application.Features.Prescriptions.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Handlers;

public sealed class UpdatePrescriptionHandler(
    IPrescriptionRepository prescriptionRepository,
    IPatientRepository patientRepository,
    ILogger<UpdatePrescriptionHandler> logger)
    : IRequestHandler<UpdatePrescriptionCommand, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(UpdatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating prescription {Id}", request.Id);

        var prescription = await prescriptionRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Prescription), request.Id);

        if (prescription.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Prescription), request.Id);

        var investigationsJson = request.Investigations.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(request.Investigations)
            : null;

        var newItems = request.Items.Select((dto, index) => PrescriptionItem.Create(
            prescription.Id,
            dto.MedicineId,
            dto.MedicineName,
            dto.MedicineForm,
            dto.Strength,
            dto.DosageUnit,
            dto.DosageMorning,
            dto.DosageAfternoon,
            dto.DosageEvening,
            dto.DosageNight,
            dto.DurationDays,
            CalculateQuantity(dto),
            dto.Frequency,
            dto.Timing,
            dto.Instructions,
            dto.RemarkEnglish,
            dto.RemarkHindi,
            dto.RemarkMarathi,
            index)).ToList();

        prescription.Update(
            request.NextVisitDate,
            request.Diagnosis,
            investigationsJson,
            request.Notes,
            request.VitalBP,
            request.VitalPulse,
            request.VitalTemp,
            request.VitalWeight,
            request.VitalSpO2,
            request.VitalRR,
            request.VitalSugar,
            newItems);
        prescriptionRepository.Update(prescription);
        await prescriptionRepository.SaveChangesAsync(cancellationToken);

        var patient = await patientRepository.GetByIdAsync(prescription.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), prescription.PatientId);

        var loaded = await prescriptionRepository.GetByIdWithDetailsAsync(prescription.Id, cancellationToken);
        return PrescriptionTranslator.ToDto(loaded!, patient);
    }

    private static decimal CalculateQuantity(CreatePrescriptionItemDto dto)
    {
        var dailyDose = (dto.DosageMorning ?? 0) + (dto.DosageAfternoon ?? 0)
                      + (dto.DosageEvening ?? 0) + (dto.DosageNight ?? 0);
        return dailyDose * dto.DurationDays;
    }
}

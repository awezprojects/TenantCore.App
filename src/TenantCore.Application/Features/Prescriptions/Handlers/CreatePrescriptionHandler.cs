using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Prescriptions.Commands;
using TenantCore.Application.Features.Prescriptions.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Prescriptions.Handlers;

public sealed class CreatePrescriptionHandler(
    IPrescriptionRepository prescriptionRepository,
    IOpdRegistrationRepository opdRepository,
    IPatientRepository patientRepository,
    ILogger<CreatePrescriptionHandler> logger)
    : IRequestHandler<CreatePrescriptionCommand, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(CreatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating prescription for OPD {OpdId}", request.OpdRegistrationId);

        var opd = await opdRepository.GetByIdAsync(request.OpdRegistrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(OpdRegistration), request.OpdRegistrationId);

        if (opd.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(OpdRegistration), request.OpdRegistrationId);

        var existing = await prescriptionRepository.GetByOpdRegistrationIdAsync(request.OpdRegistrationId, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A prescription already exists for OPD registration '{request.OpdRegistrationId}'.");

        var patient = await patientRepository.GetByIdAsync(opd.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), opd.PatientId);

        var todayCount = await prescriptionRepository.CountTodayByApplicationAsync(request.ApplicationId, cancellationToken);
        var prescriptionNumber = $"RX-{DateTime.UtcNow:yyyyMMdd}-{(todayCount + 1):D4}";

        var investigationsJson = request.Investigations.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(request.Investigations)
            : null;

        var items = request.Items.Select((dto, index) => PrescriptionItem.Create(
            Guid.Empty,
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

        var prescription = Prescription.Create(
            request.ApplicationId,
            request.OpdRegistrationId,
            opd.PatientId,
            request.DoctorUserId,
            request.DoctorName,
            prescriptionNumber,
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
            items);

        await prescriptionRepository.AddAsync(prescription, cancellationToken);

        opd.Update(opd.DoctorUserId, opd.DoctorName, opd.Fee, OpdStatus.InProgress, opd.Notes,
            opd.Weight, opd.BloodPressure, opd.PulseRate, opd.OxygenSaturation);
        opdRepository.Update(opd);

        await prescriptionRepository.SaveChangesAsync(cancellationToken);

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

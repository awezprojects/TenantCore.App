using System.Net;
using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Prescriptions.Commands;
using TenantCore.Application.Features.Prescriptions.Translators;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Prescriptions.Handlers;

public sealed class SubmitPrescriptionHandler(
    IPrescriptionRepository prescriptionRepository,
    IOpdRegistrationRepository opdRepository,
    IPatientRepository patientRepository,
    IEmailService emailService,
    ILogger<SubmitPrescriptionHandler> logger,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<SubmitPrescriptionCommand, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(SubmitPrescriptionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Submitting prescription {Id}", request.Id);

        var prescription = await prescriptionRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Prescription), request.Id);

        if (!accessValidator.CanAccess(prescription.ApplicationId))
            throw new NotFoundException(nameof(Prescription), request.Id);

        var patient = await patientRepository.GetByIdAsync(prescription.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), prescription.PatientId);

        var opd = await opdRepository.GetByIdAsync(prescription.OpdRegistrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(OpdRegistration), prescription.OpdRegistrationId);

        prescription.Submit();
        prescriptionRepository.Update(prescription);

        opd.Update(opd.DoctorUserId, opd.DoctorName, opd.Fee, OpdStatus.Completed, opd.Notes,
            opd.Weight, opd.BloodPressure, opd.PulseRate, opd.OxygenSaturation);
        opdRepository.Update(opd);

        await prescriptionRepository.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(patient.Email))
        {
            try
            {
                var html = BuildPrescriptionEmail(prescription, patient);
                await emailService.SendAsync(
                    patient.Email,
                    $"Your Prescription — {prescription.PrescriptionNumber}",
                    html,
                    ct: cancellationToken);
                prescription.MarkEmailSent(true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send prescription email for prescription {Id}", prescription.Id);
                prescription.MarkEmailSent(false);
            }
            prescriptionRepository.Update(prescription);
            await prescriptionRepository.SaveChangesAsync(cancellationToken);
        }

        var loaded = await prescriptionRepository.GetByIdWithDetailsAsync(prescription.Id, cancellationToken);
        return PrescriptionTranslator.ToDto(loaded!, patient);
    }

    private static string BuildPrescriptionEmail(Prescription prescription, Patient patient)
    {
        static string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        var rows = string.Join("", prescription.Items.Select(i =>
            $"<tr><td>{E(i.MedicineName)}</td><td>{E(i.MedicineForm.ToString())}</td>" +
            $"<td>{i.DosageMorning ?? 0}-{i.DosageAfternoon ?? 0}-{i.DosageEvening ?? 0}-{i.DosageNight ?? 0}</td>" +
            $"<td>{i.DurationDays} days</td><td>{i.Quantity} {E(i.DosageUnit)}</td>" +
            $"<td>{E(i.RemarkEnglish)}</td></tr>"));

        return $"""
            <html><body>
            <h2>Prescription: {E(prescription.PrescriptionNumber)}</h2>
            <p>Dear {E(patient.FirstName)} {E(patient.LastName)},</p>
            <p>Your prescription from Dr. {E(prescription.DoctorName)} dated {prescription.PrescribedDate:dd MMM yyyy}.</p>
            <table border='1' cellpadding='4' cellspacing='0'>
              <thead><tr><th>Medicine</th><th>Form</th><th>M-A-E-N</th><th>Duration</th><th>Qty</th><th>Remarks</th></tr></thead>
              <tbody>{rows}</tbody>
            </table>
            {(prescription.NextVisitDate.HasValue ? $"<p>Next visit: {prescription.NextVisitDate.Value:dd MMM yyyy}</p>" : "")}
            {(!string.IsNullOrWhiteSpace(prescription.Notes) ? $"<p>Notes: {E(prescription.Notes)}</p>" : "")}
            </body></html>
            """;
    }
}

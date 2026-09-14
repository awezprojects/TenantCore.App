using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Prescriptions.Commands;
using TenantCore.Application.Features.Prescriptions.Emails;
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
    IPrescriptionConfigRepository prescriptionConfigRepository,
    IPrescriptionPdfGenerator prescriptionPdfGenerator,
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
                var config = await prescriptionConfigRepository
                    .GetByApplicationIdAsync(prescription.ApplicationId, cancellationToken);
                var theme = config?.EmailTheme ?? EmailTemplateTheme.AzureClassic;

                var pdfBytes = await prescriptionPdfGenerator
                    .GenerateAsync(prescription, patient, opd, cancellationToken);

                var email = PrescriptionEmailBuilder.Build(theme, prescription, patient, opd);

                await emailService.SendAsync(
                    patient.Email,
                    email.Subject,
                    email.HtmlBody,
                    pdfBytes,
                    email.AttachmentFileName,
                    cancellationToken);

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
}

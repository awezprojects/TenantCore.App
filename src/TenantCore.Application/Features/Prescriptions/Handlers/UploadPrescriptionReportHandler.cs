using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Prescriptions.Commands;
using TenantCore.Application.Features.Prescriptions.Translators;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Handlers;

public sealed class UploadPrescriptionReportHandler(
    IPrescriptionRepository prescriptionRepository,
    IPatientRepository patientRepository,
    IFileStorageService fileStorageService,
    IPdfConversionService pdfConversionService,
    ILogger<UploadPrescriptionReportHandler> logger)
    : IRequestHandler<UploadPrescriptionReportCommand, PrescriptionReportDto>
{
    public async Task<PrescriptionReportDto> Handle(
        UploadPrescriptionReportCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Uploading report for prescription {Id}", request.PrescriptionId);

        var prescription = await prescriptionRepository.GetByIdWithDetailsAsync(request.PrescriptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Prescription), request.PrescriptionId);

        if (prescription.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Prescription), request.PrescriptionId);

        var patient = await patientRepository.GetByIdAsync(prescription.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), prescription.PatientId);

        var pdfBytes = await pdfConversionService.ConvertToPdfAsync(
            request.FileBytes, request.FileExtension, cancellationToken);

        var storedFileName = $"{Guid.NewGuid()}.pdf";
        var patientFolder = patient.Id.ToString();

        var relativePath = await fileStorageService.SaveAsync(
            new MemoryStream(pdfBytes), patientFolder, storedFileName, cancellationToken);

        var report = PrescriptionReport.Create(
            prescription.Id,
            request.FileName,
            storedFileName,
            relativePath,
            pdfBytes.Length);

        prescription.AddReport(report);
        prescriptionRepository.Update(prescription);
        await prescriptionRepository.SaveChangesAsync(cancellationToken);

        return PrescriptionTranslator.ReportToDto(report);
    }
}

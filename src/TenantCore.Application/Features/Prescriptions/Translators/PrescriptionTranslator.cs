using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Translators;

public static class PrescriptionTranslator
{
    public static PrescriptionDto ToDto(Prescription prescription, Patient patient) => new()
    {
        Id = prescription.Id,
        ApplicationId = prescription.ApplicationId,
        OpdRegistrationId = prescription.OpdRegistrationId,
        PatientId = prescription.PatientId,
        PatientName = $"{patient.FirstName} {patient.LastName}",
        PatientEmail = patient.Email,
        DoctorUserId = prescription.DoctorUserId,
        DoctorName = prescription.DoctorName,
        PrescriptionNumber = prescription.PrescriptionNumber,
        PrescribedDate = prescription.PrescribedDate,
        NextVisitDate = prescription.NextVisitDate,
        Notes = prescription.Notes,
        Status = prescription.Status,
        IsEmailSent = prescription.IsEmailSent,
        CreatedAt = prescription.CreatedAt,
        Items = prescription.Items.Select(ItemToDto).ToList(),
        Reports = prescription.Reports.Select(ReportToDto).ToList()
    };

    public static PrescriptionItemDto ItemToDto(PrescriptionItem item) => new()
    {
        Id = item.Id,
        PrescriptionId = item.PrescriptionId,
        MedicineId = item.MedicineId,
        MedicineName = item.MedicineName,
        MedicineForm = item.MedicineForm,
        DosageUnit = item.DosageUnit,
        DosageMorning = item.DosageMorning,
        DosageAfternoon = item.DosageAfternoon,
        DosageEvening = item.DosageEvening,
        DosageNight = item.DosageNight,
        DurationDays = item.DurationDays,
        Quantity = item.Quantity,
        RemarkEnglish = item.RemarkEnglish,
        RemarkHindi = item.RemarkHindi,
        RemarkMarathi = item.RemarkMarathi,
        SortOrder = item.SortOrder
    };

    public static PrescriptionReportDto ReportToDto(PrescriptionReport report) => new()
    {
        Id = report.Id,
        PrescriptionId = report.PrescriptionId,
        OriginalFileName = report.OriginalFileName,
        StoredFileName = report.StoredFileName,
        FilePath = report.FilePath,
        FileSizeBytes = report.FileSizeBytes,
        UploadedAt = report.UploadedAt
    };
}

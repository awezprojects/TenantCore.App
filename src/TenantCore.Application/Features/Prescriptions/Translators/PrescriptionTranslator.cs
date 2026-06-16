using System.Text.Json;
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
        Diagnosis = prescription.Diagnosis,
        Investigations = DeserializeStringList(prescription.Investigations),
        Notes = prescription.Notes,
        VitalBP = prescription.VitalBP,
        VitalPulse = prescription.VitalPulse,
        VitalTemp = prescription.VitalTemp,
        VitalWeight = prescription.VitalWeight,
        VitalSpO2 = prescription.VitalSpO2,
        VitalRR = prescription.VitalRR,
        VitalSugar = prescription.VitalSugar,
        Status = prescription.Status,
        IsEmailSent = prescription.IsEmailSent,
        CreatedAt = prescription.CreatedAt,
        Items = prescription.Items.Select(ItemToDto).ToList(),
        Reports = prescription.Reports.Select(ReportToDto).ToList(),
        ObstetricData = prescription.ObstetricData is not null
            ? ObstetricDataToDto(prescription.ObstetricData)
            : null
    };

    public static PrescriptionItemDto ItemToDto(PrescriptionItem item) => new()
    {
        Id = item.Id,
        PrescriptionId = item.PrescriptionId,
        MedicineId = item.MedicineId,
        MedicineName = item.MedicineName,
        MedicineForm = item.MedicineForm,
        Strength = item.Strength,
        DosageUnit = item.DosageUnit,
        DosageMorning = item.DosageMorning,
        DosageAfternoon = item.DosageAfternoon,
        DosageEvening = item.DosageEvening,
        DosageNight = item.DosageNight,
        DurationDays = item.DurationDays,
        Quantity = item.Quantity,
        Frequency = item.Frequency,
        Timing = item.Timing,
        Instructions = item.Instructions,
        RemarkEnglish = item.RemarkEnglish,
        RemarkHindi = item.RemarkHindi,
        RemarkMarathi = item.RemarkMarathi,
        SortOrder = item.SortOrder
    };

    public static ObstetricPrescriptionDataDto ObstetricDataToDto(ObstetricPrescriptionData data) => new()
    {
        Id = data.Id,
        PrescriptionId = data.PrescriptionId,
        Gravida = data.Gravida,
        Para = data.Para,
        Live = data.Live,
        Abortion = data.Abortion,
        Information = data.Information,
        MenstrualHistory = DeserializeStringList(data.MenstrualHistory),
        PastMedicalHistory = DeserializeStringList(data.PastMedicalHistory),
        FamilyHistory = DeserializeStringList(data.FamilyHistory),
        Lmp = data.Lmp,
        EddByLmp = data.EddByLmp,
        EddByUsg = data.EddByUsg
    };

    private static IReadOnlyList<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }

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

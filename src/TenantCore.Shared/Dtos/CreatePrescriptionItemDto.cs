using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public sealed record CreatePrescriptionItemDto(
    Guid MedicineId,
    string MedicineName,
    MedicineFormType MedicineForm,
    string DosageUnit,
    decimal? DosageMorning,
    decimal? DosageAfternoon,
    decimal? DosageEvening,
    decimal? DosageNight,
    int DurationDays,
    decimal Quantity,
    string? RemarkEnglish,
    string? RemarkHindi,
    string? RemarkMarathi,
    int SortOrder);

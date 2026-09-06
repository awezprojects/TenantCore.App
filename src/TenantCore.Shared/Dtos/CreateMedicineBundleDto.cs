using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public sealed record CreateMedicineBundleItemDto(
    Guid MedicineId,
    string MedicineName,
    string? GenericName,
    MedicineFormType MedicineForm,
    string? Strength,
    string DosageUnit,
    decimal? DosageMorning,
    decimal? DosageAfternoon,
    decimal? DosageEvening,
    decimal? DosageNight,
    int DurationDays,
    string? Frequency,
    string? Timing,
    string? Instructions,
    int SortOrder);

public sealed record CreateMedicineBundleDto(
    Guid DoctorUserId,
    string DoctorName,
    string Name,
    int DurationDays,
    string? Notes,
    IReadOnlyList<CreateMedicineBundleItemDto> Items);

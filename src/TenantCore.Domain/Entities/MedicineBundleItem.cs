using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class MedicineBundleItem : BaseEntity
{
    public Guid MedicineBundleId { get; private set; }
    public Guid MedicineId { get; private set; }
    public string MedicineName { get; private set; } = string.Empty;
    public string? GenericName { get; private set; }
    public MedicineFormType MedicineForm { get; private set; }
    public string? Strength { get; private set; }
    public string DosageUnit { get; private set; } = string.Empty;
    public decimal? DosageMorning { get; private set; }
    public decimal? DosageAfternoon { get; private set; }
    public decimal? DosageEvening { get; private set; }
    public decimal? DosageNight { get; private set; }
    public int DurationDays { get; private set; }
    public decimal Quantity { get; private set; }
    public string? Frequency { get; private set; }
    public string? Timing { get; private set; }
    public string? Instructions { get; private set; }
    public int SortOrder { get; private set; }

    private MedicineBundleItem() { }

    public static MedicineBundleItem Create(
        Guid medicineBundleId,
        Guid medicineId,
        string medicineName,
        string? genericName,
        MedicineFormType medicineForm,
        string? strength,
        string dosageUnit,
        decimal? dosageMorning,
        decimal? dosageAfternoon,
        decimal? dosageEvening,
        decimal? dosageNight,
        int durationDays,
        decimal quantity,
        string? frequency,
        string? timing,
        string? instructions,
        int sortOrder) => new()
    {
        Id = Guid.NewGuid(),
        MedicineBundleId = medicineBundleId,
        MedicineId = medicineId,
        MedicineName = medicineName,
        GenericName = genericName,
        MedicineForm = medicineForm,
        Strength = strength,
        DosageUnit = dosageUnit,
        DosageMorning = dosageMorning,
        DosageAfternoon = dosageAfternoon,
        DosageEvening = dosageEvening,
        DosageNight = dosageNight,
        DurationDays = durationDays,
        Quantity = quantity,
        Frequency = frequency,
        Timing = timing,
        Instructions = instructions,
        SortOrder = sortOrder,
        CreatedAt = DateTime.UtcNow
    };
}

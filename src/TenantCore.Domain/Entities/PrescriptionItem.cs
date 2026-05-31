using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class PrescriptionItem : BaseEntity
{
    public Guid PrescriptionId { get; private set; }
    public Guid MedicineId { get; private set; }
    public string MedicineName { get; private set; } = string.Empty;
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
    public string? RemarkEnglish { get; private set; }
    public string? RemarkHindi { get; private set; }
    public string? RemarkMarathi { get; private set; }
    public int SortOrder { get; private set; }

    private PrescriptionItem() { }

    public static PrescriptionItem Create(
        Guid prescriptionId,
        Guid medicineId,
        string medicineName,
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
        string? remarkEnglish,
        string? remarkHindi,
        string? remarkMarathi,
        int sortOrder) => new()
    {
        Id = Guid.NewGuid(),
        PrescriptionId = prescriptionId,
        MedicineId = medicineId,
        MedicineName = medicineName,
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
        RemarkEnglish = remarkEnglish,
        RemarkHindi = remarkHindi,
        RemarkMarathi = remarkMarathi,
        SortOrder = sortOrder,
        CreatedAt = DateTime.UtcNow
    };
}

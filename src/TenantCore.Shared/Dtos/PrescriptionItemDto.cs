using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class PrescriptionItemDto
{
    public Guid Id { get; init; }
    public Guid PrescriptionId { get; init; }
    public Guid MedicineId { get; init; }
    public string MedicineName { get; init; } = string.Empty;
    public MedicineFormType MedicineForm { get; init; }
    public string DosageUnit { get; init; } = string.Empty;
    public decimal? DosageMorning { get; init; }
    public decimal? DosageAfternoon { get; init; }
    public decimal? DosageEvening { get; init; }
    public decimal? DosageNight { get; init; }
    public int DurationDays { get; init; }
    public decimal Quantity { get; init; }
    public string? RemarkEnglish { get; init; }
    public string? RemarkHindi { get; init; }
    public string? RemarkMarathi { get; init; }
    public int SortOrder { get; init; }
}

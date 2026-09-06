using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class MedicineBundleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int DurationDays { get; init; }
    public string? Notes { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public List<MedicineBundleItemDto> Items { get; init; } = [];
}

public class MedicineBundleItemDto
{
    public Guid Id { get; init; }
    public Guid MedicineId { get; init; }
    public string MedicineName { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public MedicineFormType MedicineForm { get; init; }
    public string? Strength { get; init; }
    public string DosageUnit { get; init; } = string.Empty;
    public decimal? DosageMorning { get; init; }
    public decimal? DosageAfternoon { get; init; }
    public decimal? DosageEvening { get; init; }
    public decimal? DosageNight { get; init; }
    public int DurationDays { get; init; }
    public decimal Quantity { get; init; }
    public string? Frequency { get; init; }
    public string? Timing { get; init; }
    public string? Instructions { get; init; }
    public int SortOrder { get; init; }
}

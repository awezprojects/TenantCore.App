using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineBundles.Translators;

public static class MedicineBundleTranslator
{
    public static MedicineBundleDto ToDto(MedicineBundle entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        DurationDays = entity.DurationDays,
        Notes = entity.Notes,
        CreatedByUserId = entity.CreatedByUserId,
        CreatedByName = entity.CreatedByName,
        CreatedAt = DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc),
        Items = entity.Items
            .OrderBy(i => i.SortOrder)
            .Select(ToItemDto)
            .ToList()
    };

    public static MedicineBundleItemDto ToItemDto(MedicineBundleItem item) => new()
    {
        Id = item.Id,
        MedicineId = item.MedicineId,
        MedicineName = item.MedicineName,
        GenericName = item.GenericName,
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
        SortOrder = item.SortOrder
    };

    public static IEnumerable<MedicineBundleDto> ToDtoList(IEnumerable<MedicineBundle> entities)
        => entities.Select(ToDto);
}

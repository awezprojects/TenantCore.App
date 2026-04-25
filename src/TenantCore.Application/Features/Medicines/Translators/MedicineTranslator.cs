using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Medicines.Translators;

public static class MedicineTranslator
{
    public static MedicineDto ToDto(Medicine entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        GenericName = entity.GenericName,
        BrandName = entity.BrandName,
        Description = entity.Description,
        Composition = entity.Composition,
        Composition2 = entity.Composition2,
        Dosage = entity.Dosage,
        Form = entity.Form,
        Manufacturer = entity.Manufacturer,
        IsGeneric = entity.IsGeneric,
        PackSize = entity.PackSize,
        Uses = entity.Uses,
        SideEffects = entity.SideEffects,
        Contraindications = entity.Contraindications,
        Storage = entity.Storage,
        IsActive = entity.IsActive,
        MedicineTypeId = entity.MedicineTypeId,
        MedicineTypeName = entity.MedicineType?.Name,
        CreatedAt = entity.CreatedAt
    };

    public static IEnumerable<MedicineDto> ToDtoList(IEnumerable<Medicine> entities)
        => entities.Select(ToDto);
}

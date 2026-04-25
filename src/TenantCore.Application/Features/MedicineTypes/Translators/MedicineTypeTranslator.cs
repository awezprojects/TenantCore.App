using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineTypes.Translators;

public static class MedicineTypeTranslator
{
    public static MedicineTypeDto ToDto(MedicineType entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };

    public static IEnumerable<MedicineTypeDto> ToDtoList(IEnumerable<MedicineType> entities)
        => entities.Select(ToDto);
}

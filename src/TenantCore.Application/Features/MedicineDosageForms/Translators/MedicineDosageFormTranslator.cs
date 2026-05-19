using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineDosageForms.Translators;

public static class MedicineDosageFormTranslator
{
    public static MedicineDosageFormDto ToDto(MedicineDosageForm entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };

    public static IEnumerable<MedicineDosageFormDto> ToDtoList(IEnumerable<MedicineDosageForm> entities)
        => entities.Select(ToDto);
}

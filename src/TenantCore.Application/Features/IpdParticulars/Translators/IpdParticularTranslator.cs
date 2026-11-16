using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdParticulars.Translators;

public static class IpdParticularTranslator
{
    public static IpdParticularDto ToDto(IpdParticular entity) => new(
        entity.Id, entity.IpdRegistrationId, entity.ParticularId, entity.ParticularName,
        entity.Amount, entity.AddedByUserId, entity.CreatedAt);

    public static IEnumerable<IpdParticularDto> ToDtoList(IEnumerable<IpdParticular> entities) => entities.Select(ToDto);
}

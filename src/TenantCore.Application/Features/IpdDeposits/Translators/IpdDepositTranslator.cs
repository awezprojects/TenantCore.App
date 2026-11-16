using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdDeposits.Translators;

public static class IpdDepositTranslator
{
    public static IpdDepositDto ToDto(IpdDeposit entity) => new(
        entity.Id, entity.IpdRegistrationId, entity.Amount, entity.CollectedByUserId,
        entity.CollectedAt, entity.CounterSessionId, entity.Notes);

    public static IEnumerable<IpdDepositDto> ToDtoList(IEnumerable<IpdDeposit> entities) => entities.Select(ToDto);
}

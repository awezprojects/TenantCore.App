using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Translators;

public static class IpdBedTransferHistoryTranslator
{
    public static IpdBedTransferHistoryDto ToDto(IpdBedTransferHistory entity) => new(
        entity.Id,
        entity.BedId,
        entity.Ward.Name,
        entity.Room.RoomNumber,
        entity.Bed.BedNumber,
        entity.RatePerDayAtTime,
        entity.OccupiedFrom,
        entity.OccupiedTo,
        entity.TransferredByUserId,
        entity.Notes,
        entity.StandardChargeAmount,
        entity.AppliedChargeAmount,
        entity.AdjustmentType,
        entity.WriteOffAmount,
        entity.ManualChargeReason);

    public static IEnumerable<IpdBedTransferHistoryDto> ToDtoList(IEnumerable<IpdBedTransferHistory> entities)
        => entities.Select(ToDto);
}

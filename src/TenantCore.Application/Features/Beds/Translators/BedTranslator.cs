using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Beds.Translators;

public static class BedTranslator
{
    public static BedDto ToDto(Bed bed) => new()
    {
        Id = bed.Id,
        ApplicationId = bed.ApplicationId,
        WardId = bed.WardId,
        WardName = bed.Ward?.Name ?? string.Empty,
        RoomId = bed.RoomId,
        RoomNumber = bed.Room?.RoomNumber ?? string.Empty,
        BedNumber = bed.BedNumber,
        IsOccupied = bed.IsOccupied,
        IsActive = bed.IsActive,
        CreatedAt = bed.CreatedAt,
    };
}

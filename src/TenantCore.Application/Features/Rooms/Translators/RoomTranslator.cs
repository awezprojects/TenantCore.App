using TenantCore.Application.Features.Beds.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Rooms.Translators;

public static class RoomTranslator
{
    public static RoomDto ToDto(Room room) => new()
    {
        Id = room.Id,
        ApplicationId = room.ApplicationId,
        WardId = room.WardId,
        WardName = room.Ward?.Name ?? string.Empty,
        RoomNumber = room.RoomNumber,
        RoomType = room.RoomType,
        PricePerDay = room.PricePerDay,
        IsActive = room.IsActive,
        CreatedAt = room.CreatedAt,
        BedCount = room.Beds.Count,
        AvailableBeds = room.Beds.Count(b => !b.IsOccupied && b.IsActive),
        Beds = room.Beds.Select(BedTranslator.ToDto).ToList(),
    };
}

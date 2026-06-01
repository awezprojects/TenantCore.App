using TenantCore.Application.Features.Beds.Translators;
using TenantCore.Application.Features.Rooms.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Wards.Translators;

public static class WardTranslator
{
    public static WardDto ToDto(Ward ward) => new()
    {
        Id = ward.Id,
        ApplicationId = ward.ApplicationId,
        Name = ward.Name,
        Description = ward.Description,
        IsActive = ward.IsActive,
        CreatedAt = ward.CreatedAt,
        RoomCount = ward.Rooms.Count,
        BedCount = ward.Rooms.Sum(r => r.Beds.Count),
        AvailableBeds = ward.Rooms.Sum(r => r.Beds.Count(b => !b.IsOccupied && b.IsActive)),
        Rooms = ward.Rooms.Select(RoomTranslator.ToDto).ToList(),
    };

    public static WardDto ToDtoSimple(Ward ward) => new()
    {
        Id = ward.Id,
        ApplicationId = ward.ApplicationId,
        Name = ward.Name,
        Description = ward.Description,
        IsActive = ward.IsActive,
        CreatedAt = ward.CreatedAt,
    };
}

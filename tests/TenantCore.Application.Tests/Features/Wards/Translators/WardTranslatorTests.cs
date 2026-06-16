using FluentAssertions;
using TenantCore.Application.Features.Wards.Translators;
using TenantCore.Domain.Entities;

namespace TenantCore.Application.Tests.Features.Wards.Translators;

public class WardTranslatorTests
{
    [Fact]
    public void ToDtoSimple_WhenWardProvided_MapsBasicProperties()
    {
        var ward = Ward.Create(Guid.NewGuid(), "General Ward", "General patients");

        var result = WardTranslator.ToDtoSimple(ward);

        result.Id.Should().Be(ward.Id);
        result.ApplicationId.Should().Be(ward.ApplicationId);
        result.Name.Should().Be(ward.Name);
        result.Description.Should().Be(ward.Description);
        result.IsActive.Should().Be(ward.IsActive);
        result.CreatedAt.Should().Be(ward.CreatedAt);
        result.RoomCount.Should().Be(0);
        result.BedCount.Should().Be(0);
        result.AvailableBeds.Should().Be(0);
    }

    [Fact]
    public void ToDto_WhenWardWithRooms_MapsIncludingRoomAndBedCounts()
    {
        var ward = Ward.Create(Guid.NewGuid(), "General Ward", "General patients");
        var room = Room.Create(ward.ApplicationId, ward.Id, "101", "Private", 1500m);
        var availableBed = Bed.Create(ward.ApplicationId, ward.Id, room.Id, "B1");
        var occupiedBed = Bed.Create(ward.ApplicationId, ward.Id, room.Id, "B2");
        occupiedBed.MarkOccupied();

        SetProperty(room, nameof(Room.Ward), ward);
        SetProperty(availableBed, nameof(Bed.Ward), ward);
        SetProperty(availableBed, nameof(Bed.Room), room);
        SetProperty(occupiedBed, nameof(Bed.Ward), ward);
        SetProperty(occupiedBed, nameof(Bed.Room), room);

        room.Beds.Add(availableBed);
        room.Beds.Add(occupiedBed);
        ward.Rooms.Add(room);

        var result = WardTranslator.ToDto(ward);

        result.Id.Should().Be(ward.Id);
        result.RoomCount.Should().Be(1);
        result.BedCount.Should().Be(2);
        result.AvailableBeds.Should().Be(1);
        result.Rooms.Should().ContainSingle();
        result.Rooms[0].RoomNumber.Should().Be("101");
        result.Rooms[0].BedCount.Should().Be(2);
        result.Rooms[0].AvailableBeds.Should().Be(1);
    }

    private static void SetProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        typeof(TTarget)
            .GetProperty(propertyName)!
            .SetValue(target, value);
    }
}

using FluentAssertions;
using TenantCore.Application.Features.Rooms.Translators;
using TenantCore.Domain.Entities;

namespace TenantCore.Application.Tests.Features.Rooms.Translators;

public class RoomTranslatorTests
{
    [Fact]
    public void ToDto_WhenRoomWithNoBeds_MapsPropertiesWithZeroCounts()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var room = Room.Create(applicationId, wardId, "101", "Private", 250m);

        var result = RoomTranslator.ToDto(room);

        result.Id.Should().Be(room.Id);
        result.ApplicationId.Should().Be(applicationId);
        result.WardId.Should().Be(wardId);
        result.WardName.Should().BeEmpty();
        result.RoomNumber.Should().Be(room.RoomNumber);
        result.RoomType.Should().Be(room.RoomType);
        result.PricePerDay.Should().Be(room.PricePerDay);
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().Be(room.CreatedAt);
        result.BedCount.Should().Be(0);
        result.AvailableBeds.Should().Be(0);
        result.Beds.Should().BeEmpty();
    }
}

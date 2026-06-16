using FluentAssertions;
using TenantCore.Application.Features.Beds.Translators;
using TenantCore.Domain.Entities;

namespace TenantCore.Application.Tests.Features.Beds.Translators;

public class BedTranslatorTests
{
    [Fact]
    public void ToDto_WhenBedProvided_MapsAllProperties()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var bed = Bed.Create(applicationId, wardId, roomId, "B1");

        var result = BedTranslator.ToDto(bed);

        result.Id.Should().Be(bed.Id);
        result.ApplicationId.Should().Be(applicationId);
        result.WardId.Should().Be(wardId);
        result.WardName.Should().BeEmpty();
        result.RoomId.Should().Be(roomId);
        result.RoomNumber.Should().BeEmpty();
        result.BedNumber.Should().Be(bed.BedNumber);
        result.IsOccupied.Should().BeFalse();
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().Be(bed.CreatedAt);
    }
}

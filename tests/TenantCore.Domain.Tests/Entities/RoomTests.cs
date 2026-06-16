using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Tests.TestData;

namespace TenantCore.Domain.Tests.Entities;

public class RoomTests
{
    [Fact]
    public void Create_WhenCalled_InitializesRoomWithExpectedValues()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();

        var entity = Room.Create(
            applicationId,
            wardId,
            DomainTestData.RoomNumber,
            DomainTestData.RoomType,
            DomainTestData.RoomPricePerDay);

        entity.Id.Should().NotBeEmpty();
        entity.ApplicationId.Should().Be(applicationId);
        entity.WardId.Should().Be(wardId);
        entity.RoomNumber.Should().Be(DomainTestData.RoomNumber);
        entity.RoomType.Should().Be(DomainTestData.RoomType);
        entity.PricePerDay.Should().Be(DomainTestData.RoomPricePerDay);
        entity.IsActive.Should().BeTrue();
        entity.Beds.Should().BeEmpty();
        entity.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Update_WhenCalled_UpdatesValuesAndSetsUpdatedAt()
    {
        var entity = Room.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DomainTestData.RoomNumber,
            DomainTestData.RoomType,
            DomainTestData.RoomPricePerDay);

        entity.Update(
            DomainTestData.UpdatedRoomNumber,
            DomainTestData.UpdatedRoomType,
            DomainTestData.UpdatedRoomPricePerDay);

        entity.RoomNumber.Should().Be(DomainTestData.UpdatedRoomNumber);
        entity.RoomType.Should().Be(DomainTestData.UpdatedRoomType);
        entity.PricePerDay.Should().Be(DomainTestData.UpdatedRoomPricePerDay);
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Deactivate_WhenCalled_SetsRoomAsInactiveAndUpdatesTimestamp()
    {
        var entity = Room.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DomainTestData.RoomNumber,
            DomainTestData.RoomType,
            DomainTestData.RoomPricePerDay);

        entity.Deactivate();

        entity.IsActive.Should().BeFalse();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Activate_WhenRoomIsInactive_SetsRoomAsActiveAndUpdatesTimestamp()
    {
        var entity = Room.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DomainTestData.RoomNumber,
            DomainTestData.RoomType,
            DomainTestData.RoomPricePerDay);
        entity.Deactivate();

        entity.Activate();

        entity.IsActive.Should().BeTrue();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }
}

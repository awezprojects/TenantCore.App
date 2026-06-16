using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Tests.TestData;

namespace TenantCore.Domain.Tests.Entities;

public class BedTests
{
    [Fact]
    public void Create_WhenCalled_InitializesBedWithExpectedValues()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var roomId = Guid.NewGuid();

        var entity = Bed.Create(applicationId, wardId, roomId, DomainTestData.BedNumber);

        entity.Id.Should().NotBeEmpty();
        entity.ApplicationId.Should().Be(applicationId);
        entity.WardId.Should().Be(wardId);
        entity.RoomId.Should().Be(roomId);
        entity.BedNumber.Should().Be(DomainTestData.BedNumber);
        entity.IsOccupied.Should().BeFalse();
        entity.IsActive.Should().BeTrue();
        entity.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void MarkOccupied_WhenCalled_SetsBedAsOccupiedAndUpdatesTimestamp()
    {
        var entity = Bed.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DomainTestData.BedNumber);

        entity.MarkOccupied();

        entity.IsOccupied.Should().BeTrue();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void MarkAvailable_WhenBedIsOccupied_SetsBedAsAvailableAndUpdatesTimestamp()
    {
        var entity = Bed.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DomainTestData.BedNumber);
        entity.MarkOccupied();

        entity.MarkAvailable();

        entity.IsOccupied.Should().BeFalse();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Deactivate_WhenCalled_SetsBedAsInactiveAndUpdatesTimestamp()
    {
        var entity = Bed.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DomainTestData.BedNumber);

        entity.Deactivate();

        entity.IsActive.Should().BeFalse();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Activate_WhenBedIsInactive_SetsBedAsActiveAndUpdatesTimestamp()
    {
        var entity = Bed.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DomainTestData.BedNumber);
        entity.Deactivate();

        entity.Activate();

        entity.IsActive.Should().BeTrue();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }
}

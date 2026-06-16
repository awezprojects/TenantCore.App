using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Tests.TestData;

namespace TenantCore.Domain.Tests.Entities;

public class WardTests
{
    [Fact]
    public void Create_WhenCalled_InitializesWardWithExpectedValues()
    {
        var applicationId = Guid.NewGuid();

        var entity = Ward.Create(applicationId, DomainTestData.WardName, DomainTestData.WardDescription);

        entity.Id.Should().NotBeEmpty();
        entity.ApplicationId.Should().Be(applicationId);
        entity.Name.Should().Be(DomainTestData.WardName);
        entity.Description.Should().Be(DomainTestData.WardDescription);
        entity.IsActive.Should().BeTrue();
        entity.Rooms.Should().BeEmpty();
        entity.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Update_WhenCalled_UpdatesValuesAndSetsUpdatedAt()
    {
        var entity = Ward.Create(Guid.NewGuid(), DomainTestData.WardName, DomainTestData.WardDescription);

        entity.Update(DomainTestData.UpdatedWardName, DomainTestData.UpdatedWardDescription);

        entity.Name.Should().Be(DomainTestData.UpdatedWardName);
        entity.Description.Should().Be(DomainTestData.UpdatedWardDescription);
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Deactivate_WhenCalled_SetsWardAsInactiveAndUpdatesTimestamp()
    {
        var entity = Ward.Create(Guid.NewGuid(), DomainTestData.WardName, DomainTestData.WardDescription);

        entity.Deactivate();

        entity.IsActive.Should().BeFalse();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Activate_WhenWardIsInactive_SetsWardAsActiveAndUpdatesTimestamp()
    {
        var entity = Ward.Create(Guid.NewGuid(), DomainTestData.WardName, DomainTestData.WardDescription);
        entity.Deactivate();

        entity.Activate();

        entity.IsActive.Should().BeTrue();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }
}

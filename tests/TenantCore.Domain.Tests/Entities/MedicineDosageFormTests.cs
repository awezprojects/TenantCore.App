using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Tests.TestData;

namespace TenantCore.Domain.Tests.Entities;

public class MedicineDosageFormTests
{
    [Fact]
    public void Create_WhenCalled_InitializesMedicineDosageFormWithExpectedValues()
    {
        var entity = MedicineDosageForm.Create(
            DomainTestData.MedicineDosageFormName,
            DomainTestData.MedicineDosageFormDescription);

        entity.Id.Should().NotBeEmpty();
        entity.Name.Should().Be(DomainTestData.MedicineDosageFormName);
        entity.Description.Should().Be(DomainTestData.MedicineDosageFormDescription);
        entity.IsActive.Should().BeTrue();
        entity.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Update_WhenCalled_UpdatesValuesAndSetsUpdatedAt()
    {
        var entity = MedicineDosageForm.Create(
            DomainTestData.MedicineDosageFormName,
            DomainTestData.MedicineDosageFormDescription);

        entity.Update(
            DomainTestData.UpdatedMedicineDosageFormName,
            DomainTestData.UpdatedMedicineDosageFormDescription,
            false);

        entity.Name.Should().Be(DomainTestData.UpdatedMedicineDosageFormName);
        entity.Description.Should().Be(DomainTestData.UpdatedMedicineDosageFormDescription);
        entity.IsActive.Should().BeFalse();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Deactivate_WhenCalled_SetsMedicineDosageFormAsInactiveAndUpdatesTimestamp()
    {
        var entity = MedicineDosageForm.Create(
            DomainTestData.MedicineDosageFormName,
            DomainTestData.MedicineDosageFormDescription);

        entity.Deactivate();

        entity.IsActive.Should().BeFalse();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }
}

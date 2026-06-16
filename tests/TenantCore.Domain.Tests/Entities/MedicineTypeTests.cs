using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Tests.TestData;

namespace TenantCore.Domain.Tests.Entities;

public class MedicineTypeTests
{
    [Fact]
    public void Create_WhenCalled_InitializesMedicineTypeWithExpectedValues()
    {
        var entity = MedicineType.Create(DomainTestData.MedicineTypeName, DomainTestData.MedicineTypeDescription);

        entity.Id.Should().NotBeEmpty();
        entity.Name.Should().Be(DomainTestData.MedicineTypeName);
        entity.Description.Should().Be(DomainTestData.MedicineTypeDescription);
        entity.IsActive.Should().BeTrue();
        entity.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Update_WhenCalled_UpdatesValuesAndSetsUpdatedAt()
    {
        var entity = MedicineType.Create(DomainTestData.MedicineTypeName, DomainTestData.MedicineTypeDescription);

        entity.Update(DomainTestData.UpdatedMedicineTypeName, DomainTestData.UpdatedMedicineTypeDescription, false);

        entity.Name.Should().Be(DomainTestData.UpdatedMedicineTypeName);
        entity.Description.Should().Be(DomainTestData.UpdatedMedicineTypeDescription);
        entity.IsActive.Should().BeFalse();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }
}

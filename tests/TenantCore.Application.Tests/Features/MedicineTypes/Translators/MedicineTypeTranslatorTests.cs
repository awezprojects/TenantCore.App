using FluentAssertions;
using TenantCore.Application.Features.MedicineTypes.Translators;
using TenantCore.Application.Tests.TestData;
using TenantCore.Domain.Entities;

namespace TenantCore.Application.Tests.Features.MedicineTypes.Translators;

public class MedicineTypeTranslatorTests
{
    [Fact]
    public void ToDto_WhenEntityProvided_MapsAllProperties()
    {
        var entity = MedicineType.Create(ApplicationTestData.MedicineTypeName, ApplicationTestData.MedicineTypeDescription);

        var result = MedicineTypeTranslator.ToDto(entity);

        result.Id.Should().Be(entity.Id);
        result.Name.Should().Be(entity.Name);
        result.Description.Should().Be(entity.Description);
        result.IsActive.Should().Be(entity.IsActive);
        result.CreatedAt.Should().Be(entity.CreatedAt);
    }

    [Fact]
    public void ToDtoList_WhenEntitiesProvided_MapsAllItems()
    {
        var entities = new[]
        {
            MedicineType.Create(ApplicationTestData.MedicineTypeName, ApplicationTestData.MedicineTypeDescription),
            MedicineType.Create(ApplicationTestData.SecondMedicineTypeName, ApplicationTestData.SecondMedicineTypeDescription)
        };

        var result = MedicineTypeTranslator.ToDtoList(entities).ToList();

        result.Should().HaveCount(entities.Length);
        result.Select(x => x.Name).Should().BeEquivalentTo(entities.Select(x => x.Name));
    }
}

using FluentAssertions;
using TenantCore.Application.Features.MedicineDosageForms.Translators;
using TenantCore.Domain.Entities;

namespace TenantCore.Application.Tests.Features.MedicineDosageForms.Translators;

public class MedicineDosageFormTranslatorTests
{
    [Fact]
    public void ToDto_WhenEntityProvided_MapsAllProperties()
    {
        var entity = MedicineDosageForm.Create("Tablet", "Oral solid dosage form");

        var result = MedicineDosageFormTranslator.ToDto(entity);

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
            MedicineDosageForm.Create("Tablet", "Oral solid dosage form"),
            MedicineDosageForm.Create("Capsule", "Encapsulated dosage form")
        };

        var result = MedicineDosageFormTranslator.ToDtoList(entities).ToList();

        result.Should().HaveCount(entities.Length);
        result.Select(x => x.Name).Should().BeEquivalentTo(entities.Select(x => x.Name));
        result.Select(x => x.Description).Should().BeEquivalentTo(entities.Select(x => x.Description));
    }
}

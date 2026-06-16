using FluentAssertions;
using TenantCore.Application.Features.DosageRemarks.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.DosageRemarks.Translators;

public class DosageRemarkTranslatorTests
{
    [Fact]
    public void ToDto_WhenEntityProvided_MapsAllProperties()
    {
        var entity = DosageRemark.Create(
            Guid.NewGuid(),
            MedicineFormType.Drops,
            "Use twice daily",
            "दिन में दो बार उपयोग करें",
            "दिवसातून दोनदा वापरा");

        var result = DosageRemarkTranslator.ToDto(entity);

        result.Id.Should().Be(entity.Id);
        result.ApplicationId.Should().Be(entity.ApplicationId);
        result.MedicineForm.Should().Be(entity.MedicineForm);
        result.RemarkEnglish.Should().Be(entity.RemarkEnglish);
        result.RemarkHindi.Should().Be(entity.RemarkHindi);
        result.RemarkMarathi.Should().Be(entity.RemarkMarathi);
        result.IsActive.Should().Be(entity.IsActive);
        result.CreatedAt.Should().Be(entity.CreatedAt);
    }
}

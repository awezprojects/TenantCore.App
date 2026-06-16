using FluentAssertions;
using TenantCore.Application.Features.Obstetrics.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.Obstetrics.Translators;

public class ObstetricDatesTranslatorTests
{
    [Fact]
    public void ToDto_WhenDataHasAllFields_MapsAllProperties()
    {
        var prescriptionId = Guid.NewGuid();
        var lmp    = new DateOnly(2024, 1, 15);
        var eddUsg = new DateOnly(2024, 10, 22);
        var data = ObstetricPrescriptionData.CreateOrUpdate(prescriptionId, new UpsertObstetricPrescriptionDataDto(
            Gravida: 2, Para: 1, Live: 1, Abortion: 0,
            Information: null, MenstrualHistory: null, PastMedicalHistory: null, FamilyHistory: null,
            Lmp: lmp, EddByUsg: eddUsg));

        var result = ObstetricDatesTranslator.ToDto(data);

        result.Id.Should().Be(data.Id);
        result.PrescriptionId.Should().Be(prescriptionId);
        result.Lmp.Should().Be(lmp);
        result.EddByLmp.Should().Be(lmp.AddDays(280));
        result.EddByUsg.Should().Be(eddUsg);
    }

    [Fact]
    public void ToDto_WhenDataHasNoLmpOrEdd_MapsNullableFieldsAsNull()
    {
        var prescriptionId = Guid.NewGuid();
        var data = ObstetricPrescriptionData.CreateOrUpdate(prescriptionId, new UpsertObstetricPrescriptionDataDto(
            Gravida: null, Para: null, Live: null, Abortion: null,
            Information: null, MenstrualHistory: null, PastMedicalHistory: null, FamilyHistory: null));

        var result = ObstetricDatesTranslator.ToDto(data);

        result.PrescriptionId.Should().Be(prescriptionId);
        result.Lmp.Should().BeNull();
        result.EddByLmp.Should().BeNull();
        result.EddByUsg.Should().BeNull();
    }
}

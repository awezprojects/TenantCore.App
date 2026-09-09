using FluentAssertions;
using TenantCore.Application.Features.Prescriptions.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.Prescriptions.Translators;

public class PrescriptionTranslatorTests
{
    private static UpsertObstetricPrescriptionDataDto BuildDto(
        IReadOnlyList<HistoryItemSelectionDto>? menstrualHistory) => new(
        Gravida: 1, Para: 0, Live: 0, Abortion: 0,
        Information: null,
        MenstrualHistory: menstrualHistory,
        PastMedicalHistory: null,
        FamilyHistory: null);

    [Fact]
    public void ObstetricDataToDto_NewFormatSelections_RoundTripsValueAndPrintFlagExactly()
    {
        var selections = new List<HistoryItemSelectionDto>
        {
            new("3-4/30 Regular", true),
            new("scanty flow", false)
        };
        var entity = ObstetricPrescriptionData.CreateOrUpdate(Guid.NewGuid(), BuildDto(selections));

        var dto = PrescriptionTranslator.ObstetricDataToDto(entity);

        dto.MenstrualHistory.Should().HaveCount(2);
        dto.MenstrualHistory[0].Should().Be(new HistoryItemSelectionDto("3-4/30 Regular", true));
        dto.MenstrualHistory[1].Should().Be(new HistoryItemSelectionDto("scanty flow", false));
    }

    [Fact]
    public void ObstetricDataToDto_EmptySection_ReturnsEmptyList()
    {
        var entity = ObstetricPrescriptionData.CreateOrUpdate(Guid.NewGuid(), BuildDto(null));

        var dto = PrescriptionTranslator.ObstetricDataToDto(entity);

        dto.MenstrualHistory.Should().BeEmpty();
    }

    [Fact]
    public void ObstetricDataToDto_MapsCoreFields()
    {
        var prescriptionId = Guid.NewGuid();
        var entity = ObstetricPrescriptionData.CreateOrUpdate(prescriptionId, BuildDto(null));

        var dto = PrescriptionTranslator.ObstetricDataToDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.PrescriptionId.Should().Be(prescriptionId);
        dto.Gravida.Should().Be(1);
    }
}

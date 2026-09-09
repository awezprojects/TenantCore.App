using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Domain.Tests.Entities;

public class ObstetricPrescriptionDataTests
{
    private static UpsertObstetricPrescriptionDataDto BuildDto(
        IReadOnlyList<HistoryItemSelectionDto>? menstrualHistory = null) => new(
        Gravida: 2, Para: 1, Live: 1, Abortion: 0,
        Information: "info",
        MenstrualHistory: menstrualHistory,
        PastMedicalHistory: null,
        FamilyHistory: null);

    [Fact]
    public void CreateOrUpdate_MixedPrintFlags_SerializesAndDeserializesEveryFlagCorrectly()
    {
        var selections = new List<HistoryItemSelectionDto>
        {
            new("3-4/30 Regular", true),
            new("scanty flow", false)
        };
        var prescriptionId = Guid.NewGuid();

        var entity = ObstetricPrescriptionData.CreateOrUpdate(prescriptionId, BuildDto(selections));
        var result = ObstetricPrescriptionData.DeserializeSelections(entity.MenstrualHistory);

        result.Should().HaveCount(2);
        result[0].Should().Be(new HistoryItemSelectionDto("3-4/30 Regular", true));
        result[1].Should().Be(new HistoryItemSelectionDto("scanty flow", false));
    }

    [Fact]
    public void CreateOrUpdate_EmptyList_StoresNullColumn()
    {
        var entity = ObstetricPrescriptionData.CreateOrUpdate(Guid.NewGuid(), BuildDto([]));

        entity.MenstrualHistory.Should().BeNull();
    }

    [Fact]
    public void CreateOrUpdate_NullList_StoresNullColumn()
    {
        var entity = ObstetricPrescriptionData.CreateOrUpdate(Guid.NewGuid(), BuildDto(null));

        entity.MenstrualHistory.Should().BeNull();
    }

    [Fact]
    public void Update_ReplacesExistingSelectionsAndSetsUpdatedAt()
    {
        var entity = ObstetricPrescriptionData.CreateOrUpdate(
            Guid.NewGuid(), BuildDto([new HistoryItemSelectionDto("irregular cycle", true)]));

        entity.Update(BuildDto([new HistoryItemSelectionDto("amenorrhea", false)]));

        var result = ObstetricPrescriptionData.DeserializeSelections(entity.MenstrualHistory);
        result.Should().ContainSingle();
        result[0].Should().Be(new HistoryItemSelectionDto("amenorrhea", false));
        entity.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void DeserializeSelections_LegacyPlainStringArrayJson_DefaultsEveryItemToPrintTrue()
    {
        const string legacyJson = "[\"3-4/30 Regular\",\"scanty flow\",\"irregular cycle\"]";

        var result = ObstetricPrescriptionData.DeserializeSelections(legacyJson);

        result.Should().HaveCount(3);
        result.Should().OnlyContain(x => x.PrintOnPrescription);
        result.Select(x => x.Value).Should().ContainInOrder("3-4/30 Regular", "scanty flow", "irregular cycle");
    }

    [Fact]
    public void DeserializeSelections_NewFormatJson_PreservesEachPrintFlag()
    {
        const string json = "[{\"Value\":\"amenorrhea\",\"PrintOnPrescription\":false},{\"Value\":\"clots present\",\"PrintOnPrescription\":true}]";

        var result = ObstetricPrescriptionData.DeserializeSelections(json);

        result.Should().HaveCount(2);
        result[0].Should().Be(new HistoryItemSelectionDto("amenorrhea", false));
        result[1].Should().Be(new HistoryItemSelectionDto("clots present", true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeserializeSelections_NullOrWhitespace_ReturnsEmptyList(string? json)
    {
        ObstetricPrescriptionData.DeserializeSelections(json).Should().BeEmpty();
    }
}

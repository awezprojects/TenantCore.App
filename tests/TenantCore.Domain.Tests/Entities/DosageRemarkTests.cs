using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Tests.TestData;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Tests.Entities;

public class DosageRemarkTests
{
    [Fact]
    public void Create_WhenCalled_InitializesDosageRemarkWithExpectedValues()
    {
        var applicationId = Guid.NewGuid();

        var entity = DosageRemark.Create(
            applicationId,
            MedicineFormType.Tab,
            DomainTestData.DosageRemarkEnglish,
            DomainTestData.DosageRemarkHindi,
            DomainTestData.DosageRemarkMarathi);

        entity.Id.Should().NotBeEmpty();
        entity.ApplicationId.Should().Be(applicationId);
        entity.MedicineForm.Should().Be(MedicineFormType.Tab);
        entity.RemarkEnglish.Should().Be(DomainTestData.DosageRemarkEnglish);
        entity.RemarkHindi.Should().Be(DomainTestData.DosageRemarkHindi);
        entity.RemarkMarathi.Should().Be(DomainTestData.DosageRemarkMarathi);
        entity.IsActive.Should().BeTrue();
        entity.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Update_WhenCalled_UpdatesValuesAndSetsUpdatedAt()
    {
        var entity = DosageRemark.Create(
            Guid.NewGuid(),
            MedicineFormType.Tab,
            DomainTestData.DosageRemarkEnglish,
            DomainTestData.DosageRemarkHindi,
            DomainTestData.DosageRemarkMarathi);

        entity.Update(
            MedicineFormType.Capsule,
            DomainTestData.UpdatedDosageRemarkEnglish,
            DomainTestData.UpdatedDosageRemarkHindi,
            DomainTestData.UpdatedDosageRemarkMarathi,
            false);

        entity.MedicineForm.Should().Be(MedicineFormType.Capsule);
        entity.RemarkEnglish.Should().Be(DomainTestData.UpdatedDosageRemarkEnglish);
        entity.RemarkHindi.Should().Be(DomainTestData.UpdatedDosageRemarkHindi);
        entity.RemarkMarathi.Should().Be(DomainTestData.UpdatedDosageRemarkMarathi);
        entity.IsActive.Should().BeFalse();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }
}

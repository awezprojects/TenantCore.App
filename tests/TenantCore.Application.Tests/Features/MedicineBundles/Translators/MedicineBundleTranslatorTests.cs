using FluentAssertions;
using TenantCore.Application.Features.MedicineBundles.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.MedicineBundles.Translators;

public class MedicineBundleTranslatorTests
{
    [Fact]
    public void ToDto_MapsAllFieldsAndOrdersItemsBySortOrder()
    {
        var applicationId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var medicineId1 = Guid.NewGuid();
        var medicineId2 = Guid.NewGuid();

        var itemB = MedicineBundleItem.Create(
            Guid.Empty, medicineId2, "Aspirin", null, MedicineFormType.Tab, "75mg", "tablet",
            1, null, null, null, 30, 30, "OD", "Morning", null, sortOrder: 1);
        var itemA = MedicineBundleItem.Create(
            Guid.Empty, medicineId1, "Folvite", null, MedicineFormType.Tab, "5mg", "tablet",
            1, null, null, null, 30, 30, "OD", "Morning", "After meals", sortOrder: 0);

        var bundle = MedicineBundle.Create(
            applicationId, "1st Trimester Care", 30, "Standard set", doctorId, "Dr. Smith", [itemB, itemA]);

        var dto = MedicineBundleTranslator.ToDto(bundle);

        dto.Name.Should().Be("1st Trimester Care");
        dto.DurationDays.Should().Be(30);
        dto.Notes.Should().Be("Standard set");
        dto.CreatedByUserId.Should().Be(doctorId);
        dto.CreatedByName.Should().Be("Dr. Smith");
        dto.Items.Should().HaveCount(2);
        dto.Items[0].MedicineName.Should().Be("Folvite");
        dto.Items[1].MedicineName.Should().Be("Aspirin");
    }
}

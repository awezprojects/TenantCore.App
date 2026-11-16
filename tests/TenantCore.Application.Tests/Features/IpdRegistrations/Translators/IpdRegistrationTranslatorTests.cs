using FluentAssertions;
using TenantCore.Application.Features.IpdRegistrations.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.IpdRegistrations.Translators;

public class IpdRegistrationTranslatorTests
{
    [Fact]
    public void ToDto_ValidEntity_MapsAllProperties()
    {
        var bedId = Guid.NewGuid();
        var patient = Patient.Create(Guid.NewGuid(), "Jane", "Doe", null, Gender.Female, "9876543210", null, null, null, null);
        var entity = IpdRegistration.Create(
            Guid.NewGuid(), patient.Id, Guid.NewGuid(), "Dr. Smith", "IPD-20261112-0001",
            bedId, "Ward A", "101", "B1", 2000m, "Admit for observation");
        typeof(IpdRegistration).GetProperty(nameof(IpdRegistration.Patient))!.SetValue(entity, patient);

        var dto = IpdRegistrationTranslator.ToDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.ApplicationId.Should().Be(entity.ApplicationId);
        dto.PatientId.Should().Be(patient.Id);
        dto.PatientName.Should().Be("Jane Doe");
        dto.DoctorName.Should().Be("Dr. Smith");
        dto.AdmissionNumber.Should().Be("IPD-20261112-0001");
        dto.CurrentBedId.Should().Be(bedId);
        dto.WardName.Should().Be("Ward A");
        dto.RoomNumber.Should().Be("101");
        dto.BedNumber.Should().Be("B1");
        dto.InitialFee.Should().Be(2000m);
        dto.Status.Should().Be(IpdStatus.Admitted);
        dto.AdmissionNotes.Should().Be("Admit for observation");
    }

    [Fact]
    public void ToDto_NoBedAllocated_CurrentBedIdIsNull()
    {
        var patient = Patient.Create(Guid.NewGuid(), "Jane", "Doe", null, Gender.Female, "9876543210", null, null, null, null);
        var entity = IpdRegistration.Create(
            Guid.NewGuid(), patient.Id, Guid.NewGuid(), "Dr. Smith", "IPD-20261112-0001",
            null, null, null, null, 2000m, null);
        typeof(IpdRegistration).GetProperty(nameof(IpdRegistration.Patient))!.SetValue(entity, patient);

        var dto = IpdRegistrationTranslator.ToDto(entity);

        dto.CurrentBedId.Should().BeNull();
        dto.WardName.Should().BeNull();
    }
}

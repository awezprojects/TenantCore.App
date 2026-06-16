using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Tests.TestData;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Tests.Entities;

public class PatientTests
{
    [Fact]
    public void Create_WhenCalled_InitializesPatientWithExpectedValues()
    {
        var applicationId = Guid.NewGuid();
        var dateOfBirth = new DateOnly(1990, 5, 15);

        var entity = Patient.Create(
            applicationId,
            DomainTestData.PatientFirstName,
            DomainTestData.PatientLastName,
            dateOfBirth,
            Gender.Male,
            DomainTestData.PatientPhone,
            DomainTestData.PatientEmail,
            DomainTestData.PatientAadhaarNumber,
            DomainTestData.PatientPhotoUrl,
            DomainTestData.PatientAddress,
            DomainTestData.PatientBloodGroup,
            DomainTestData.PatientEmergencyContactName,
            DomainTestData.PatientEmergencyContactPhone,
            DomainTestData.PatientKnownAllergies,
            DomainTestData.PatientMedicalHistory);

        entity.Id.Should().NotBeEmpty();
        entity.ApplicationId.Should().Be(applicationId);
        entity.FirstName.Should().Be(DomainTestData.PatientFirstName);
        entity.LastName.Should().Be(DomainTestData.PatientLastName);
        entity.DateOfBirth.Should().Be(dateOfBirth);
        entity.Gender.Should().Be(Gender.Male);
        entity.PhoneNumber.Should().Be(DomainTestData.PatientPhone);
        entity.Email.Should().Be(DomainTestData.PatientEmail);
        entity.AadhaarNumber.Should().Be(DomainTestData.PatientAadhaarNumber);
        entity.PhotoUrl.Should().Be(DomainTestData.PatientPhotoUrl);
        entity.Address.Should().Be(DomainTestData.PatientAddress);
        entity.BloodGroup.Should().Be(DomainTestData.PatientBloodGroup);
        entity.EmergencyContactName.Should().Be(DomainTestData.PatientEmergencyContactName);
        entity.EmergencyContactPhone.Should().Be(DomainTestData.PatientEmergencyContactPhone);
        entity.KnownAllergies.Should().Be(DomainTestData.PatientKnownAllergies);
        entity.MedicalHistory.Should().Be(DomainTestData.PatientMedicalHistory);
        entity.IsActive.Should().BeTrue();
        entity.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Update_WhenCalled_UpdatesValuesAndSetsUpdatedAt()
    {
        var entity = Patient.Create(
            Guid.NewGuid(),
            DomainTestData.PatientFirstName,
            DomainTestData.PatientLastName,
            new DateOnly(1990, 5, 15),
            Gender.Male,
            DomainTestData.PatientPhone,
            DomainTestData.PatientEmail,
            DomainTestData.PatientAadhaarNumber,
            DomainTestData.PatientPhotoUrl,
            DomainTestData.PatientAddress,
            DomainTestData.PatientBloodGroup,
            DomainTestData.PatientEmergencyContactName,
            DomainTestData.PatientEmergencyContactPhone,
            DomainTestData.PatientKnownAllergies,
            DomainTestData.PatientMedicalHistory);

        var updatedDateOfBirth = new DateOnly(1992, 8, 20);

        entity.Update(
            DomainTestData.UpdatedPatientFirstName,
            DomainTestData.UpdatedPatientLastName,
            updatedDateOfBirth,
            Gender.Female,
            DomainTestData.UpdatedPatientPhone,
            DomainTestData.UpdatedPatientEmail,
            DomainTestData.UpdatedPatientAadhaarNumber,
            DomainTestData.UpdatedPatientPhotoUrl,
            DomainTestData.UpdatedPatientAddress,
            DomainTestData.UpdatedPatientBloodGroup,
            DomainTestData.UpdatedPatientEmergencyContactName,
            DomainTestData.UpdatedPatientEmergencyContactPhone,
            DomainTestData.UpdatedPatientKnownAllergies,
            DomainTestData.UpdatedPatientMedicalHistory);

        entity.FirstName.Should().Be(DomainTestData.UpdatedPatientFirstName);
        entity.LastName.Should().Be(DomainTestData.UpdatedPatientLastName);
        entity.DateOfBirth.Should().Be(updatedDateOfBirth);
        entity.Gender.Should().Be(Gender.Female);
        entity.PhoneNumber.Should().Be(DomainTestData.UpdatedPatientPhone);
        entity.Email.Should().Be(DomainTestData.UpdatedPatientEmail);
        entity.AadhaarNumber.Should().Be(DomainTestData.UpdatedPatientAadhaarNumber);
        entity.PhotoUrl.Should().Be(DomainTestData.UpdatedPatientPhotoUrl);
        entity.Address.Should().Be(DomainTestData.UpdatedPatientAddress);
        entity.BloodGroup.Should().Be(DomainTestData.UpdatedPatientBloodGroup);
        entity.EmergencyContactName.Should().Be(DomainTestData.UpdatedPatientEmergencyContactName);
        entity.EmergencyContactPhone.Should().Be(DomainTestData.UpdatedPatientEmergencyContactPhone);
        entity.KnownAllergies.Should().Be(DomainTestData.UpdatedPatientKnownAllergies);
        entity.MedicalHistory.Should().Be(DomainTestData.UpdatedPatientMedicalHistory);
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void UpdatePhotoUrl_WhenCalled_UpdatesPhotoUrlAndSetsUpdatedAt()
    {
        var entity = Patient.Create(
            Guid.NewGuid(),
            DomainTestData.PatientFirstName,
            DomainTestData.PatientLastName,
            new DateOnly(1990, 5, 15),
            Gender.Male,
            DomainTestData.PatientPhone,
            DomainTestData.PatientEmail,
            DomainTestData.PatientAadhaarNumber,
            DomainTestData.PatientPhotoUrl,
            DomainTestData.PatientAddress,
            DomainTestData.PatientBloodGroup,
            DomainTestData.PatientEmergencyContactName,
            DomainTestData.PatientEmergencyContactPhone,
            DomainTestData.PatientKnownAllergies,
            DomainTestData.PatientMedicalHistory);

        entity.UpdatePhotoUrl(DomainTestData.UpdatedPatientPhotoUrl);

        entity.PhotoUrl.Should().Be(DomainTestData.UpdatedPatientPhotoUrl);
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Deactivate_WhenCalled_SetsPatientAsInactiveAndUpdatesTimestamp()
    {
        var entity = Patient.Create(
            Guid.NewGuid(),
            DomainTestData.PatientFirstName,
            DomainTestData.PatientLastName,
            new DateOnly(1990, 5, 15),
            Gender.Male,
            DomainTestData.PatientPhone,
            DomainTestData.PatientEmail,
            DomainTestData.PatientAadhaarNumber,
            DomainTestData.PatientPhotoUrl,
            DomainTestData.PatientAddress,
            DomainTestData.PatientBloodGroup,
            DomainTestData.PatientEmergencyContactName,
            DomainTestData.PatientEmergencyContactPhone,
            DomainTestData.PatientKnownAllergies,
            DomainTestData.PatientMedicalHistory);

        entity.Deactivate();

        entity.IsActive.Should().BeFalse();
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }
}

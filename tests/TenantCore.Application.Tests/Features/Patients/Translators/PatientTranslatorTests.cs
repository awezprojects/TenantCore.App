using FluentAssertions;
using TenantCore.Application.Features.Patients.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Patients.Translators;

public class PatientTranslatorTests
{
    [Fact]
    public void ToDto_WhenShowFullAadhaarTrue_ReturnsFullAadhaar()
    {
        var patient = CreatePatient(aadhaarNumber: "123456789012");

        var result = PatientTranslator.ToDto(patient, showFullAadhaar: true);

        result.AadhaarNumber.Should().Be("123456789012");
    }

    [Fact]
    public void ToDto_WhenShowFullAadhaarFalse_MasksAadhaarWithXXXX()
    {
        var patient = CreatePatient(aadhaarNumber: "123456789012");

        var result = PatientTranslator.ToDto(patient, showFullAadhaar: false);

        result.AadhaarNumber.Should().Be("XXXX-XXXX-9012");
    }

    [Fact]
    public void ToDto_WhenAadhaarIsNull_ReturnsNull()
    {
        var patient = CreatePatient(aadhaarNumber: null);

        var result = PatientTranslator.ToDto(patient);

        result.AadhaarNumber.Should().BeNull();
    }

    [Fact]
    public void ToDto_WhenCalled_MapsAllProperties()
    {
        var patient = CreatePatient(aadhaarNumber: "123456789012");

        var result = PatientTranslator.ToDto(patient, showFullAadhaar: true, hasLmpRecord: true, hasActiveTenure: true);

        result.Id.Should().Be(patient.Id);
        result.ApplicationId.Should().Be(patient.ApplicationId);
        result.FirstName.Should().Be(patient.FirstName);
        result.LastName.Should().Be(patient.LastName);
        result.DateOfBirth.Should().Be(patient.DateOfBirth);
        result.Gender.Should().Be(patient.Gender);
        result.PhoneNumber.Should().Be(patient.PhoneNumber);
        result.Email.Should().Be(patient.Email);
        result.AadhaarNumber.Should().Be(patient.AadhaarNumber);
        result.PhotoUrl.Should().Be(patient.PhotoUrl);
        result.Address.Should().Be(patient.Address);
        result.BloodGroup.Should().Be(patient.BloodGroup);
        result.EmergencyContactName.Should().Be(patient.EmergencyContactName);
        result.EmergencyContactPhone.Should().Be(patient.EmergencyContactPhone);
        result.KnownAllergies.Should().Be(patient.KnownAllergies);
        result.MedicalHistory.Should().Be(patient.MedicalHistory);
        result.IsActive.Should().Be(patient.IsActive);
        result.CreatedAt.Should().Be(patient.CreatedAt);
        result.HasLmpRecord.Should().BeTrue();
        result.HasActiveTenure.Should().BeTrue();
    }

    private static Patient CreatePatient(string? aadhaarNumber)
        => Patient.Create(
            Guid.NewGuid(),
            "Jane",
            "Doe",
            new DateOnly(1995, 4, 20),
            Gender.Female,
            "+1234567890",
            "jane@example.com",
            aadhaarNumber,
            "https://cdn.example.com/patient.jpg",
            "123 Main St",
            "O+",
            "John Doe",
            "+1987654321",
            "Peanuts",
            "Asthma");
}

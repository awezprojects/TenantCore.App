using FluentAssertions;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Validators;
using TenantCore.Shared.Dtos.Onboarding;

namespace TenantCore.Application.Tests.Features.Onboarding.Validators;

public class SubmitClinicOnboardingCommandValidatorTests
{
    private readonly SubmitClinicOnboardingCommandValidator _validator = new();

    private static SubmitClinicOnboardingCommand CreateCommand(
        Guid? userId = null, string requesterName = "Requester", string requesterEmail = "requester@example.test",
        string clinicName = "Sunrise Clinic", string preferredClinicCode = "SUNRISE", string address = "123 Main St",
        string city = "Pune", string state = "MH", string pincode = "411001", string requesterPhone = "9876543210",
        string? clinicContactNumber = null, string? officialEmail = null, string? website = null,
        string doctorName = "Dr. Jane", string medicalRegistrationNumber = "MR12345", string medicalCouncil = "MCI",
        int expectedStaffCount = 5, string? referralSource = null, string? notes = null) => new(
        userId ?? Guid.NewGuid(), requesterName, requesterEmail, new SubmitClinicOnboardingRequest
        {
            ClinicName = clinicName,
            PreferredClinicCode = preferredClinicCode,
            Address = address,
            City = city,
            State = state,
            Pincode = pincode,
            RequesterPhone = requesterPhone,
            ClinicContactNumber = clinicContactNumber,
            OfficialEmail = officialEmail,
            Website = website,
            DoctorName = doctorName,
            MedicalRegistrationNumber = medicalRegistrationNumber,
            MedicalCouncil = medicalCouncil,
            ExpectedStaffCount = expectedStaffCount,
            ReferralSource = referralSource,
            Notes = notes
        });

    [Fact]
    public void Validate_ValidCommand_ReturnsValid()
    {
        _validator.Validate(CreateCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyUserId_ReturnsError()
    {
        var result = _validator.Validate(CreateCommand(userId: Guid.Empty));
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData("AB")]      // 2 chars — below minimum
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")] // 21 chars — above maximum
    public void Validate_ClinicCodeOutsideLengthRange_ReturnsError(string code)
    {
        var result = _validator.Validate(CreateCommand(preferredClinicCode: code));
        result.Errors.Should().Contain(e => e.PropertyName == "Request.PreferredClinicCode");
    }

    [Theory]
    [InlineData("ABC")]                  // 3 chars — minimum
    [InlineData("ABCDEFGHIJKLMNOPQRST")] // 20 chars — maximum
    public void Validate_ClinicCodeAtBoundary_ReturnsValid(string code)
    {
        var result = _validator.Validate(CreateCommand(preferredClinicCode: code));
        result.Errors.Should().NotContain(e => e.PropertyName == "Request.PreferredClinicCode");
    }

    [Fact]
    public void Validate_ClinicCodeWithInvalidCharacters_ReturnsError()
    {
        var result = _validator.Validate(CreateCommand(preferredClinicCode: "SUN RISE!"));
        result.Errors.Should().Contain(e => e.PropertyName == "Request.PreferredClinicCode");
    }

    [Theory]
    [InlineData("41100")]   // 5 digits
    [InlineData("4110011")] // 7 digits
    [InlineData("41100A")]  // non-digit
    public void Validate_PincodeNotSixDigits_ReturnsError(string pincode)
    {
        var result = _validator.Validate(CreateCommand(pincode: pincode));
        result.Errors.Should().Contain(e => e.PropertyName == "Request.Pincode");
    }

    [Fact]
    public void Validate_PincodeExactlySixDigits_ReturnsValid()
    {
        var result = _validator.Validate(CreateCommand(pincode: "411001"));
        result.Errors.Should().NotContain(e => e.PropertyName == "Request.Pincode");
    }

    [Fact]
    public void Validate_RequesterPhoneEmpty_ReturnsError()
    {
        var result = _validator.Validate(CreateCommand(requesterPhone: string.Empty));
        result.Errors.Should().Contain(e => e.PropertyName == "Request.RequesterPhone");
    }

    [Fact]
    public void Validate_RequesterPhoneWithInvalidCharacters_ReturnsError()
    {
        var result = _validator.Validate(CreateCommand(requesterPhone: "98abc12345"));
        result.Errors.Should().Contain(e => e.PropertyName == "Request.RequesterPhone");
    }

    [Fact]
    public void Validate_ClinicContactNumberNull_ReturnsValid()
    {
        var result = _validator.Validate(CreateCommand(clinicContactNumber: null));
        result.Errors.Should().NotContain(e => e.PropertyName == "Request.ClinicContactNumber");
    }

    [Fact]
    public void Validate_OfficialEmailInvalid_ReturnsError()
    {
        var result = _validator.Validate(CreateCommand(officialEmail: "not-an-email"));
        result.Errors.Should().Contain(e => e.PropertyName == "Request.OfficialEmail");
    }

    [Fact]
    public void Validate_OfficialEmailNull_ReturnsValid()
    {
        var result = _validator.Validate(CreateCommand(officialEmail: null));
        result.Errors.Should().NotContain(e => e.PropertyName == "Request.OfficialEmail");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public void Validate_ExpectedStaffCountOutsideRange_ReturnsError(int count)
    {
        var result = _validator.Validate(CreateCommand(expectedStaffCount: count));
        result.Errors.Should().Contain(e => e.PropertyName == "Request.ExpectedStaffCount");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    public void Validate_ExpectedStaffCountAtBoundary_ReturnsValid(int count)
    {
        var result = _validator.Validate(CreateCommand(expectedStaffCount: count));
        result.Errors.Should().NotContain(e => e.PropertyName == "Request.ExpectedStaffCount");
    }

    [Fact]
    public void Validate_ClinicNameEmpty_ReturnsError()
    {
        var result = _validator.Validate(CreateCommand(clinicName: string.Empty));
        result.Errors.Should().Contain(e => e.PropertyName == "Request.ClinicName");
    }

    [Fact]
    public void Validate_ClinicNameTooLong_ReturnsError()
    {
        var result = _validator.Validate(CreateCommand(clinicName: new string('a', 201)));
        result.Errors.Should().Contain(e => e.PropertyName == "Request.ClinicName");
    }

    [Fact]
    public void Validate_DoctorNameEmpty_ReturnsError()
    {
        var result = _validator.Validate(CreateCommand(doctorName: string.Empty));
        result.Errors.Should().Contain(e => e.PropertyName == "Request.DoctorName");
    }

    [Fact]
    public void Validate_MedicalRegistrationNumberEmpty_ReturnsError()
    {
        var result = _validator.Validate(CreateCommand(medicalRegistrationNumber: string.Empty));
        result.Errors.Should().Contain(e => e.PropertyName == "Request.MedicalRegistrationNumber");
    }

    [Fact]
    public void Validate_MedicalCouncilEmpty_ReturnsError()
    {
        var result = _validator.Validate(CreateCommand(medicalCouncil: string.Empty));
        result.Errors.Should().Contain(e => e.PropertyName == "Request.MedicalCouncil");
    }
}

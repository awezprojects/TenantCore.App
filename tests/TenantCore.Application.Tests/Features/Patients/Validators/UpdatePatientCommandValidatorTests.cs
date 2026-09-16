using FluentAssertions;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Application.Features.Patients.Validators;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Patients.Validators;

public class UpdatePatientCommandValidatorTests
{
    private readonly UpdatePatientCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValid()
    {
        var command = CreateCommand();

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ReturnsError()
    {
        var command = CreateCommand(id: Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Id");
    }

    [Fact]
    public void Validate_WhenDateOfBirthIsInFuture_ReturnsError()
    {
        var command = CreateCommand(dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("DateOfBirth"));
    }

    [Fact]
    public void Validate_WhenDateOfBirthIsMoreThan120YearsAgo_ReturnsError()
    {
        var command = CreateCommand(dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-121));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("DateOfBirth"));
    }

    [Fact]
    public void Validate_WhenDateOfBirthIsExactly120YearsAgo_ReturnsValid()
    {
        var command = CreateCommand(dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-120));

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("999")]
    [InlineData("+91 98123-45699")]
    public void Validate_WhenPhoneNumberIsNotTenDigits_ReturnsError(string phoneNumber)
    {
        var command = CreateCommand(phoneNumber: phoneNumber);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void Validate_WhenEmergencyContactPhoneIsNotTenDigits_ReturnsError()
    {
        var command = CreateCommand(emergencyContactPhone: "abcXYZ!!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "EmergencyContactPhone");
    }

    [Fact]
    public void Validate_WhenFirstNameContainsScriptTag_ReturnsError()
    {
        var command = CreateCommand(firstName: "<script>alert('XSS')</script>");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "FirstName");
    }

    [Fact]
    public void Validate_WhenAddressContainsHtmlTags_ReturnsError()
    {
        var command = CreateCommand(address: "<img src=x onerror=alert(document.cookie)>");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Address");
    }

    private static UpdatePatientCommand CreateCommand(
        Guid? id = null,
        string firstName = "Jane",
        string phoneNumber = "9812345670",
        string? emergencyContactPhone = null,
        string? address = "123 Main St",
        DateOnly? dateOfBirth = null)
        => new(
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            firstName,
            "Doe",
            dateOfBirth ?? new DateOnly(1995, 4, 20),
            Gender.Female,
            phoneNumber,
            "jane@example.com",
            "123456789012",
            null,
            address,
            EmergencyContactPhone: emergencyContactPhone);
}

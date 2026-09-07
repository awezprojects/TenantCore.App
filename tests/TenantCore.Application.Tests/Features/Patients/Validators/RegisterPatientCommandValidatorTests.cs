using FluentAssertions;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Application.Features.Patients.Validators;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Patients.Validators;

public class RegisterPatientCommandValidatorTests
{
    private readonly RegisterPatientCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValid()
    {
        var command = CreateCommand();

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenFirstNameIsEmpty_ReturnsError()
    {
        var command = CreateCommand(firstName: string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "FirstName");
    }

    [Fact]
    public void Validate_WhenLastNameIsEmpty_ReturnsError()
    {
        var command = CreateCommand(lastName: string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "LastName");
    }

    [Fact]
    public void Validate_WhenPhoneNumberIsEmpty_ReturnsError()
    {
        var command = CreateCommand(phoneNumber: string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void Validate_WhenAadhaarNumberIsNotTwelveDigits_ReturnsError()
    {
        var command = CreateCommand(aadhaarNumber: "12345");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "AadhaarNumber");
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_ReturnsError()
    {
        var command = CreateCommand(email: "not-an-email");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WhenDateOfBirthIsToday_ReturnsValid()
    {
        var command = CreateCommand(dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow));

        _validator.Validate(command).IsValid.Should().BeTrue();
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
    public void Validate_WhenDateOfBirthIsExactly120YearsAgo_ReturnsValid()
    {
        var command = CreateCommand(dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-120));

        _validator.Validate(command).IsValid.Should().BeTrue();
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
    public void Validate_WhenDateOfBirthIsNull_ReturnsValid()
    {
        var command = CreateCommand(dateOfBirth: null);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    private static RegisterPatientCommand CreateCommand(
        string firstName = "Jane",
        string lastName = "Doe",
        string phoneNumber = "+1234567890",
        string? email = "jane@example.com",
        string? aadhaarNumber = "123456789012",
        DateOnly? dateOfBirth = null)
        => new(
            Guid.NewGuid(),
            firstName,
            lastName,
            dateOfBirth ?? new DateOnly(1995, 4, 20),
            Gender.Female,
            phoneNumber,
            email,
            aadhaarNumber,
            null,
            "123 Main St");
}

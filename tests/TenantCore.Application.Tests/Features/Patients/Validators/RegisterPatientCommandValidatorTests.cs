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

    private static RegisterPatientCommand CreateCommand(
        string firstName = "Jane",
        string lastName = "Doe",
        string phoneNumber = "+1234567890",
        string? email = "jane@example.com",
        string? aadhaarNumber = "123456789012")
        => new(
            Guid.NewGuid(),
            firstName,
            lastName,
            new DateOnly(1995, 4, 20),
            Gender.Female,
            phoneNumber,
            email,
            aadhaarNumber,
            null,
            "123 Main St");
}

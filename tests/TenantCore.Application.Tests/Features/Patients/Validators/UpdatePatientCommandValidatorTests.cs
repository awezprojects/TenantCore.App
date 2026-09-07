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

    private static UpdatePatientCommand CreateCommand(
        Guid? id = null,
        string phoneNumber = "+1234567890",
        DateOnly? dateOfBirth = null)
        => new(
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            "Jane",
            "Doe",
            dateOfBirth ?? new DateOnly(1995, 4, 20),
            Gender.Female,
            phoneNumber,
            "jane@example.com",
            "123456789012",
            null,
            "123 Main St");
}

using FluentAssertions;
using TenantCore.Application.Features.Obstetrics.Commands;
using TenantCore.Application.Features.Obstetrics.Validators;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.Obstetrics.Validators;

public class SetObstetricLmpCommandValidatorTests
{
    private readonly SetObstetricLmpCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValidResult()
    {
        var command = new SetObstetricLmpCommand(
            Guid.NewGuid(),
            new SetLmpRequest { Lmp = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30) },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenLmpIsToday_ReturnsValidResult()
    {
        var command = new SetObstetricLmpCommand(
            Guid.NewGuid(),
            new SetLmpRequest { Lmp = DateOnly.FromDateTime(DateTime.UtcNow) },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenPrescriptionIdIsEmpty_ReturnsValidationError()
    {
        var command = new SetObstetricLmpCommand(
            Guid.Empty,
            new SetLmpRequest { Lmp = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10) },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(SetObstetricLmpCommand.PrescriptionId));
    }

    [Fact]
    public void Validate_WhenApplicationIdIsEmpty_ReturnsValidationError()
    {
        var command = new SetObstetricLmpCommand(
            Guid.NewGuid(),
            new SetLmpRequest { Lmp = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10) },
            Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(SetObstetricLmpCommand.ApplicationId));
    }

    [Fact]
    public void Validate_WhenLmpIsInFuture_ReturnsValidationError()
    {
        var command = new SetObstetricLmpCommand(
            Guid.NewGuid(),
            new SetLmpRequest { Lmp = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1) },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("LMP date cannot be in the future"));
    }
}

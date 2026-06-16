using FluentAssertions;
using TenantCore.Application.Features.Obstetrics.Commands;
using TenantCore.Application.Features.Obstetrics.Validators;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.Obstetrics.Validators;

public class SetObstetricEddByUsgCommandValidatorTests
{
    private readonly SetObstetricEddByUsgCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValidResult()
    {
        var command = new SetObstetricEddByUsgCommand(
            Guid.NewGuid(),
            new SetEddByUsgRequest { EddByUsg = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30) },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEddIsToday_ReturnsValidResult()
    {
        var command = new SetObstetricEddByUsgCommand(
            Guid.NewGuid(),
            new SetEddByUsgRequest { EddByUsg = DateOnly.FromDateTime(DateTime.UtcNow) },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenPrescriptionIdIsEmpty_ReturnsValidationError()
    {
        var command = new SetObstetricEddByUsgCommand(
            Guid.Empty,
            new SetEddByUsgRequest { EddByUsg = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30) },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(SetObstetricEddByUsgCommand.PrescriptionId));
    }

    [Fact]
    public void Validate_WhenApplicationIdIsEmpty_ReturnsValidationError()
    {
        var command = new SetObstetricEddByUsgCommand(
            Guid.NewGuid(),
            new SetEddByUsgRequest { EddByUsg = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30) },
            Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(SetObstetricEddByUsgCommand.ApplicationId));
    }

    [Fact]
    public void Validate_WhenEddIsInPast_ReturnsValidationError()
    {
        var command = new SetObstetricEddByUsgCommand(
            Guid.NewGuid(),
            new SetEddByUsgRequest { EddByUsg = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1) },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("EDD by USG cannot be in the past"));
    }
}

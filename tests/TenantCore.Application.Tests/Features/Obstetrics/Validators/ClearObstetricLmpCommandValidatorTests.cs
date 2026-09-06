using FluentAssertions;
using TenantCore.Application.Features.Obstetrics.Commands;
using TenantCore.Application.Features.Obstetrics.Validators;

namespace TenantCore.Application.Tests.Features.Obstetrics.Validators;

public class ClearObstetricLmpCommandValidatorTests
{
    private readonly ClearObstetricLmpCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValidResult()
    {
        var command = new ClearObstetricLmpCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenPrescriptionIdIsEmpty_ReturnsValidationError()
    {
        var command = new ClearObstetricLmpCommand(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ClearObstetricLmpCommand.PrescriptionId));
    }

    [Fact]
    public void Validate_WhenApplicationIdIsEmpty_ReturnsValidationError()
    {
        var command = new ClearObstetricLmpCommand(Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ClearObstetricLmpCommand.ApplicationId));
    }
}

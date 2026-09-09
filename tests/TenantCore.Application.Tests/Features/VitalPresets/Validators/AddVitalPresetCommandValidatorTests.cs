using FluentAssertions;
using TenantCore.Application.Features.VitalPresets.Commands;
using TenantCore.Application.Features.VitalPresets.Validators;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.VitalPresets.Validators;

public class AddVitalPresetCommandValidatorTests
{
    private readonly AddVitalPresetCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = new AddVitalPresetCommand(Guid.NewGuid(), VitalFieldType.Bp, "120/80");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyApplicationId_FailsWithError()
    {
        var command = new AddVitalPresetCommand(Guid.Empty, VitalFieldType.Bp, "120/80");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ApplicationId"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyValue_FailsWithError(string value)
    {
        var command = new AddVitalPresetCommand(Guid.NewGuid(), VitalFieldType.Bp, value);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ValueAtMaxLength_PassesValidation()
    {
        var command = new AddVitalPresetCommand(Guid.NewGuid(), VitalFieldType.Bp, new string('a', 64));

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValueExceedsMaxLength_FailsWithError()
    {
        var command = new AddVitalPresetCommand(Guid.NewGuid(), VitalFieldType.Bp, new string('a', 65));

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}

using FluentAssertions;
using TenantCore.Application.Features.ClinicSettings.Commands;
using TenantCore.Application.Features.ClinicSettings.Validators;

namespace TenantCore.Application.Tests.Features.ClinicSettings.Validators;

public class UpdateClinicFeatureFlagsCommandValidatorTests
{
    private readonly UpdateClinicFeatureFlagsCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = new UpdateClinicFeatureFlagsCommand(Guid.NewGuid(), true);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyApplicationId_FailsWithError()
    {
        var command = new UpdateClinicFeatureFlagsCommand(Guid.Empty, true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ApplicationId"));
    }
}

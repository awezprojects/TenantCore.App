using FluentAssertions;
using TenantCore.Application.Features.ClinicSettings.Commands;
using TenantCore.Application.Features.ClinicSettings.Validators;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.ClinicSettings.Validators;

public class UpdateClinicFeatureFlagsCommandValidatorTests
{
    private readonly UpdateClinicFeatureFlagsCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = new UpdateClinicFeatureFlagsCommand(Guid.NewGuid(), true, true, null, null);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyApplicationId_FailsWithError()
    {
        var command = new UpdateClinicFeatureFlagsCommand(Guid.Empty, true, true, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ApplicationId"));
    }

    [Fact]
    public void Validate_ValidIpdBillingSettings_PassesValidation()
    {
        var command = new UpdateClinicFeatureFlagsCommand(
            Guid.NewGuid(), true, true, RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidIpdRoomBillingCycle_FailsWithError()
    {
        var command = new UpdateClinicFeatureFlagsCommand(Guid.NewGuid(), true, true, (RoomBillingCycle)99, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("IpdRoomBillingCycle"));
    }

    [Fact]
    public void Validate_NegativeGraceWindow_FailsWithError()
    {
        var command = new UpdateClinicFeatureFlagsCommand(
            Guid.NewGuid(), true, true, null, null, IpdShiftGraceWindowMinutes: -5);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("IpdShiftGraceWindowMinutes"));
    }

    [Fact]
    public void Validate_GraceWindowAboveUpperBound_FailsWithError()
    {
        // OBS-23 (14-Nov QA): 100000 minutes was accepted before this fix.
        var command = new UpdateClinicFeatureFlagsCommand(
            Guid.NewGuid(), true, true, null, null, IpdShiftGraceWindowMinutes: 100000, IpdShiftGraceRule: IpdShiftGraceRule.NewRoomOnly);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("IpdShiftGraceWindowMinutes"));
    }

    [Fact]
    public void Validate_GraceWindowAtUpperBound_PassesValidation()
    {
        var command = new UpdateClinicFeatureFlagsCommand(
            Guid.NewGuid(), true, true, null, null, IpdShiftGraceWindowMinutes: 1440, IpdShiftGraceRule: IpdShiftGraceRule.NewRoomOnly);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GraceWindowOnWithoutGraceRule_FailsWithError()
    {
        var command = new UpdateClinicFeatureFlagsCommand(
            Guid.NewGuid(), true, true, null, null, IpdShiftGraceWindowMinutes: 60, IpdShiftGraceRule: null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_GraceWindowOnWithGraceRule_PassesValidation()
    {
        var command = new UpdateClinicFeatureFlagsCommand(
            Guid.NewGuid(), true, true, null, null, IpdShiftGraceWindowMinutes: 60, IpdShiftGraceRule: IpdShiftGraceRule.NewRoomOnly);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GraceWindowOff_PassesValidationEvenWithoutGraceRule()
    {
        var command = new UpdateClinicFeatureFlagsCommand(
            Guid.NewGuid(), true, true, null, null, IpdShiftGraceWindowMinutes: 0, IpdShiftGraceRule: null);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeCancelWindow_FailsWithError()
    {
        // DEF-IPD-26 (15-Nov).
        var command = new UpdateClinicFeatureFlagsCommand(
            Guid.NewGuid(), true, true, null, null, IpdCancelWindowMinutes: -5);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("IpdCancelWindowMinutes"));
    }

    [Fact]
    public void Validate_CancelWindowAboveUpperBound_FailsWithError()
    {
        var command = new UpdateClinicFeatureFlagsCommand(
            Guid.NewGuid(), true, true, null, null, IpdCancelWindowMinutes: 1441);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("IpdCancelWindowMinutes"));
    }

    [Fact]
    public void Validate_CancelWindowAtUpperBound_PassesValidation()
    {
        var command = new UpdateClinicFeatureFlagsCommand(
            Guid.NewGuid(), true, true, null, null, IpdCancelWindowMinutes: 1440);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CancelWindowOff_PassesValidation()
    {
        var command = new UpdateClinicFeatureFlagsCommand(
            Guid.NewGuid(), true, true, null, null, IpdCancelWindowMinutes: 0);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CancelWindowValidValue_PassesValidation()
    {
        var command = new UpdateClinicFeatureFlagsCommand(
            Guid.NewGuid(), true, true, null, null, IpdCancelWindowMinutes: 60);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }
}

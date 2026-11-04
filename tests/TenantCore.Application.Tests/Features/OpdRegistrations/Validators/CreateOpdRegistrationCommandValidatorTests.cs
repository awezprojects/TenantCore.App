using FluentAssertions;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Validators;

namespace TenantCore.Application.Tests.Features.OpdRegistrations.Validators;

public class CreateOpdRegistrationCommandValidatorTests
{
    private readonly CreateOpdRegistrationCommandValidator _validator = new();

    private static CreateOpdRegistrationCommand ValidCommand(
        decimal? fee = 100, decimal? weight = null, string? bloodPressure = null,
        int? pulseRate = null, decimal? oxygenSaturation = null, decimal? temperature = null,
        int? respiratoryRate = null, decimal? sugar = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", fee, null,
            weight, bloodPressure, pulseRate, oxygenSaturation, temperature, respiratoryRate, sugar,
            Guid.NewGuid());

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValid()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    // DEF-OPD-10 — no upper bound previously let an absurd fee auto-collect into the session total.
    [Fact]
    public void Validate_WhenFeeExceedsMax_ReturnsError()
    {
        var result = _validator.Validate(ValidCommand(fee: 99_999_999_999));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Fee");
    }

    // DEF-OPD-11 — vitals previously only checked ">= 0", so clinically impossible values were accepted.
    [Theory]
    [InlineData(250)]   // O2 Saturation %
    public void Validate_WhenOxygenSaturationOutOfRange_ReturnsError(decimal value)
    {
        var result = _validator.Validate(ValidCommand(oxygenSaturation: value));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "OxygenSaturation");
    }

    [Fact]
    public void Validate_WhenTemperatureOutOfRange_ReturnsError()
    {
        var result = _validator.Validate(ValidCommand(temperature: 500));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Temperature");
    }

    [Fact]
    public void Validate_WhenPulseRateOutOfRange_ReturnsError()
    {
        var result = _validator.Validate(ValidCommand(pulseRate: 900));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "PulseRate");
    }

    [Fact]
    public void Validate_WhenWeightOutOfRange_ReturnsError()
    {
        var result = _validator.Validate(ValidCommand(weight: 9999));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Weight");
    }

    [Fact]
    public void Validate_WhenBloodPressureNotNumericFormat_ReturnsError()
    {
        var result = _validator.Validate(ValidCommand(bloodPressure: "abc"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "BloodPressure");
    }

    [Fact]
    public void Validate_WhenBloodPressureIsValidFormat_ReturnsValid()
        => _validator.Validate(ValidCommand(bloodPressure: "120/80")).IsValid.Should().BeTrue();
}

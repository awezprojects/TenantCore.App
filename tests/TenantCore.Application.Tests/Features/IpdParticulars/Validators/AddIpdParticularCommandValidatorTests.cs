using FluentAssertions;
using TenantCore.Application.Features.IpdParticulars.Commands;
using TenantCore.Application.Features.IpdParticulars.Validators;

namespace TenantCore.Application.Tests.Features.IpdParticulars.Validators;

public class AddIpdParticularCommandValidatorTests
{
    private readonly AddIpdParticularCommandValidator _validator = new();

    private static AddIpdParticularCommand ValidCommand(decimal amount = 300m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), amount, Guid.NewGuid());

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ZeroAmount_PassesValidation()
        => _validator.Validate(ValidCommand(0)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_NegativeAmount_FailsWithError()
        => _validator.Validate(ValidCommand(-1)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyParticularId_FailsWithError()
        => _validator.Validate(ValidCommand() with { ParticularId = Guid.Empty }).IsValid.Should().BeFalse();
}

using FluentAssertions;
using TenantCore.Application.Features.IpdPayments.Commands;
using TenantCore.Application.Features.IpdPayments.Validators;

namespace TenantCore.Application.Tests.Features.IpdPayments.Validators;

public class ApplyIpdDiscountCommandValidatorTests
{
    private readonly ApplyIpdDiscountCommandValidator _validator = new();

    private static ApplyIpdDiscountCommand ValidCommand(decimal discount = 100m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), discount, Guid.NewGuid());

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ZeroDiscount_PassesValidation()
        => _validator.Validate(ValidCommand(0)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_NegativeDiscount_FailsWithError()
        => _validator.Validate(ValidCommand(-1)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyDiscountedByUserId_FailsWithError()
        => _validator.Validate(ValidCommand() with { DiscountedByUserId = Guid.Empty }).IsValid.Should().BeFalse();
}

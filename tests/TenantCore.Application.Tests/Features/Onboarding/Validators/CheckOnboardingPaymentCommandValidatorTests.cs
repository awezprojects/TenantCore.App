using FluentAssertions;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Validators;

namespace TenantCore.Application.Tests.Features.Onboarding.Validators;

public class CheckOnboardingPaymentCommandValidatorTests
{
    private readonly CheckOnboardingPaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ReturnsValid() =>
        _validator.Validate(new CheckOnboardingPaymentCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyId_ReturnsError() =>
        _validator.Validate(new CheckOnboardingPaymentCommand(Guid.Empty, Guid.NewGuid())).Errors.Should().Contain(e => e.PropertyName == "Id");

    [Fact]
    public void Validate_EmptyUserId_ReturnsError() =>
        _validator.Validate(new CheckOnboardingPaymentCommand(Guid.NewGuid(), Guid.Empty)).Errors.Should().Contain(e => e.PropertyName == "UserId");
}
